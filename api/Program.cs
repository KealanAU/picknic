using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Email;
using Picknic.Api.Endpoints;
using Picknic.Api.Models;
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

builder.Services.AddDbContext<PicknicDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=picknic.db"));

builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<PicknicDbContext>();

// Guests authenticate on a separate JWT scheme, not Identity.
var guestOpts = builder.Configuration.GetSection(GuestTokenOptions.SectionName).Get<GuestTokenOptions>()
    ?? new GuestTokenOptions();
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
builder.Services.AddSingleton<JoinSecretProtector>();
builder.Services.AddSingleton<EventLinks>();
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Dev convenience — create the schema. Use EF migrations for production.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PicknicDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).WithName("Health");

app.MapGroup("/api/auth").MapIdentityApi<AppUser>();

app.MapEventEndpoints();
app.MapUploadEndpoints();
app.MapInviteEndpoints();
app.MapCheckoutEndpoints();

app.Run();
