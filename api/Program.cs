using System.Net;
using System.Threading.RateLimiting;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Email;
using Picknic.Api.Endpoints;
using Picknic.Api.FilmProcessing;
using Picknic.Api.Models;
using Picknic.Api.Notifications;
using Picknic.Api.Payments;
using Picknic.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<GuestTokenOptions>(
    builder.Configuration.GetSection(GuestTokenOptions.SectionName));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<EventGridOptions>(
    builder.Configuration.GetSection(EventGridOptions.SectionName));

builder.Services.AddDbContext<PicknicDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")
        ?? "Host=localhost;Database=picknic;Username=picknic;Password=picknic",
        npgsql => npgsql.EnableRetryOnFailure()));

// Probe the database so Container Apps liveness/readiness reflects real backend
// health rather than always reporting ok.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PicknicDbContext>("database");

// Running behind Container Apps ingress: trust the proxy's forwarded headers so
// RemoteIpAddress is the real client (used to partition rate limits). Only the
// proxies/networks listed in ForwardedHeaders:Known* are trusted — without them
// the framework default (loopback only) stands, so an external caller can't
// spoof X-Forwarded-For to dodge the IP-partitioned rate limits.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var knownProxies = (builder.Configuration["ForwardedHeaders:KnownProxies"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var knownNetworks = (builder.Configuration["ForwardedHeaders:KnownNetworks"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (knownProxies.Length == 0 && knownNetworks.Length == 0) return;

    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    foreach (var proxy in knownProxies)
        o.KnownProxies.Add(IPAddress.Parse(proxy));
    foreach (var network in knownNetworks)
        o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
});

// Per-client rate limits on the abusable public endpoints (auth, join,
// uploads, checkout). Partitioned by client IP; anything over the window gets
// a 429.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string ClientKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    static Func<HttpContext, RateLimitPartition<string>> PerIp(int permitLimit) =>
        ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
            });

    // Covers login (credential stuffing) and the email-sending endpoints
    // (register, resendConfirmationEmail, forgotPassword).
    options.AddPolicy("auth", PerIp(10));
    options.AddPolicy("join", PerIp(10));
    options.AddPolicy("invite", PerIp(10));
    options.AddPolicy("upload", PerIp(60));
    options.AddPolicy("photos", PerIp(30));
    options.AddPolicy("checkout", PerIp(5));
});

builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<PicknicDbContext>();

builder.Services.AddTransient<IEmailSender<AppUser>, IdentityEmailSender>();

// Guests authenticate on a separate JWT scheme, not Identity.
var guestOpts = builder.Configuration.GetSection(GuestTokenOptions.SectionName).Get<GuestTokenOptions>()
    ?? new GuestTokenOptions();
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(guestOpts.SigningKey))
    throw new InvalidOperationException("Auth:Guest:SigningKey must be configured outside Development.");
builder.Services.AddAuthentication()
    .AddJwtBearer("Guest", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = guestOpts.Issuer,
            ValidateAudience = true,
            ValidAudience = guestOpts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = guestOpts.SecurityKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Host", p => p
        .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
        .RequireAuthenticatedUser())
    .AddPolicy("Guest", p => p
        .AddAuthenticationSchemes("Guest")
        .RequireAuthenticatedUser()
        .RequireClaim(GuestTokenService.EventClaim));

builder.Services.AddScoped<GuestTokenService>();
builder.Services.AddScoped<BlobSasService>();

// Film-look pipeline: codec + processor are stateless (singletons); the developer
// depends on the scoped BlobSasService, so it's scoped too.
builder.Services.AddSingleton<IImageCodec, SkiaImageCodec>();
builder.Services.AddSingleton<IFilmProcessor, FilmProcessor>();
builder.Services.AddScoped<FilmDeveloper>();
builder.Services.AddSingleton<JoinSecretProtector>();
builder.Services.AddSingleton<EventLinks>();
var emailOpts = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
    ?? new EmailOptions();
if (emailOpts.Enabled)
    builder.Services.AddSingleton<IEmailSender, AcsEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

builder.Services.AddHostedService<RevealNotificationService>();

// Persist the Data Protection key ring to blob storage (encrypted with a Key
// Vault key) so JoinSecretProtector survives restarts and is shared across
// replicas. Without config (dev) the default per-machine key store is used.
var dpBlobUri = builder.Configuration["DataProtection:BlobUri"];
var dpKeyVaultKeyId = builder.Configuration["DataProtection:KeyVaultKeyId"];
if (!string.IsNullOrWhiteSpace(dpBlobUri) && !string.IsNullOrWhiteSpace(dpKeyVaultKeyId))
{
    var credential = new DefaultAzureCredential();
    builder.Services.AddDataProtection()
        .SetApplicationName("picknic")
        .PersistKeysToAzureBlobStorage(new Uri(dpBlobUri), credential)
        .ProtectKeysWithAzureKeyVault(new Uri(dpKeyVaultKeyId), credential);
}

// Lock CORS to the configured web origin(s). Development may leave
// Cors:AllowedOrigins empty to allow any origin; outside Development an unset
// value is a misconfiguration we fail fast on rather than silently falling open.
var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (corsOrigins.Length > 0)
        p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
    else
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// Apply pending EF migrations on startup, guarded by a Postgres advisory lock so
// that when replicas start together only one runs migrations and the rest wait.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PicknicDbContext>();
    if (db.Database.IsNpgsql())
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await using (var lockCmd = conn.CreateCommand())
            {
                lockCmd.CommandText = "SELECT pg_advisory_lock(4242424242)";
                await lockCmd.ExecuteNonQueryAsync();
            }
            await db.Database.MigrateAsync();
        }
        finally
        {
            await using var unlockCmd = conn.CreateCommand();
            unlockCmd.CommandText = "SELECT pg_advisory_unlock(4242424242)";
            await unlockCmd.ExecuteNonQueryAsync();
        }
    }
    else
    {
        // A non-Postgres provider means integration tests: the Npgsql migrations
        // (and advisory lock) don't apply there, so build the schema from the model.
        await db.Database.EnsureCreatedAsync();
    }

    // Ensure the photos container exists when running against the emulator.
    await scope.ServiceProvider.GetRequiredService<BlobSasService>().InitializeAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseForwardedHeaders();
app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<AuthInputSanitizationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status == HealthStatus.Healthy ? "ok" : "unhealthy",
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        });
    },
}).WithName("Health");

app.MapGroup("/api/auth").RequireRateLimiting("auth").MapIdentityApi<AppUser>();

app.MapEventEndpoints();
app.MapUploadEndpoints();
app.MapEventGridEndpoints();
app.MapFilmEndpoints();
app.MapGuestEndpoints();
app.MapInviteEndpoints();
app.MapCheckoutEndpoints();

app.Run();

// Exposes the implicit entry-point class to WebApplicationFactory-based tests.
public partial class Program { }
