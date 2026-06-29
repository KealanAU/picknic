using System.Collections.Concurrent;
using Picknic.Api.Models;
using Picknic.Api.Payments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// In-memory store for the scaffold. Swap for a real DB + Azure Blob Storage.
var events = new ConcurrentDictionary<string, Event>();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

// Create an event (a "roll" that develops at revealAt).
app.MapPost("/api/events", (CreateEvent req) =>
{
    var ev = new Event(
        Id: Guid.NewGuid().ToString("n"),
        Code: req.Code.ToUpperInvariant(),
        Name: req.Name,
        RevealAt: req.RevealAt);
    events[ev.Code] = ev;
    return Results.Created($"/api/events/{ev.Code}", ev);
})
.WithName("CreateEvent");

// Join / look up an event by its code.
app.MapGet("/api/events/{code}", (string code) =>
    events.TryGetValue(code.ToUpperInvariant(), out var ev)
        ? Results.Ok(ev)
        : Results.NotFound())
.WithName("GetEvent");

// Photos are only revealed once the roll has "developed".
app.MapGet("/api/events/{code}/photos", (string code) =>
{
    if (!events.TryGetValue(code.ToUpperInvariant(), out var ev))
        return Results.NotFound();

    return DateTimeOffset.UtcNow < ev.RevealAt
        ? Results.Ok(new { revealed = false, ev.RevealAt })
        : Results.Ok(new { revealed = true, photos = ev.Photos });
})
.WithName("GetPhotos");

// Optional payments — see Payments/CheckoutEndpoints.cs.
app.MapCheckoutEndpoints();

app.Run();
