using System.Text.Json;
using System.Text.Json.Nodes;

namespace Picknic.Api.Auth;

public sealed class AuthInputSanitizationMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> ValidatedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "newEmail",
        "userName",
        "username",
        "newUserName",
        "newUsername",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsAuthRequest(context.Request))
        {
            await next(context);
            return;
        }

        // Rejecting (never rewriting) means "user<x>@a.com" can't be silently
        // registered as a different address than the one the user typed.
        var invalidField = await FindInvalidFieldAsync(context.Request);
        if (invalidField is not null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "One or more validation errors occurred.",
                status = StatusCodes.Status400BadRequest,
                errors = new Dictionary<string, string[]>
                {
                    [invalidField] = [$"'{invalidField}' contains characters that are not allowed."],
                },
            }, JsonOptions, "application/problem+json");
            return;
        }

        if (!IsLoginRequest(context.Request))
        {
            await next(context);
            return;
        }

        // Login failures collapse to one generic message so the response doesn't
        // reveal whether the account exists or is locked out. Register (and the
        // rest) keep Identity's problem details — password-policy feedback matters.
        var originalBody = context.Response.Body;
        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;

        try
        {
            await next(context);

            context.Response.Body = originalBody;
            if (context.Response.StatusCode
                is StatusCodes.Status400BadRequest or StatusCodes.Status401Unauthorized)
            {
                context.Response.ContentLength = null;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Invalid email or password",
                    status = context.Response.StatusCode,
                }, JsonOptions, "application/problem+json");
                return;
            }

            bufferedBody.Position = 0;
            await bufferedBody.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    public static bool ContainsDangerousCharacters(string value)
    {
        foreach (var ch in value)
            if (char.IsControl(ch) || ch is '<' or '>' or '"' or '\'' or '`' or ';' or '\\')
                return true;
        return false;
    }

    private static bool IsAuthRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api/auth")
        && HttpMethods.IsPost(request.Method)
        && request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsLoginRequest(HttpRequest request) =>
        request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase);

    private static async Task<string?> FindInvalidFieldAsync(HttpRequest request)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var raw = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(raw)) return null;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            // Malformed JSON is the model binder's 400 to produce.
            return null;
        }

        return root is null ? null : FindInvalidField(root);
    }

    private static string? FindInvalidField(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (value is JsonValue jsonValue
                        && ValidatedFields.Contains(key)
                        && jsonValue.TryGetValue<string>(out var raw))
                    {
                        if (ContainsDangerousCharacters(raw)) return key;
                    }
                    else if (value is not null && FindInvalidField(value) is { } nested)
                    {
                        return nested;
                    }
                }
                break;

            case JsonArray array:
                foreach (var value in array)
                    if (value is not null && FindInvalidField(value) is { } nested)
                        return nested;
                break;
        }

        return null;
    }
}
