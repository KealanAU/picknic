using System.Text;
using Microsoft.AspNetCore.Http;
using Picknic.Api.Auth;

namespace Picknic.Api.Tests;

public class AuthInputSanitizationMiddlewareTests
{
    [Theory]
    [InlineData("User<Name>@Example.com")]
    [InlineData("user'name@example.com")]
    [InlineData("user;name@example.com")]
    [InlineData("user\\name@example.com")]
    [InlineData("user`name@example.com")]
    [InlineData("user\u0000name@example.com")]
    public void Flags_dangerous_characters(string value)
    {
        Assert.True(AuthInputSanitizationMiddleware.ContainsDangerousCharacters(value));
    }

    [Theory]
    [InlineData("safe.user+tag@example.com")]
    [InlineData("user-name_123@example.co.uk")]
    public void Allows_safe_identifiers(string value)
    {
        Assert.False(AuthInputSanitizationMiddleware.ContainsDangerousCharacters(value));
    }

    [Fact]
    public async Task Rejects_dangerous_email_naming_the_field_without_invoking_the_endpoint()
    {
        var endpointCalled = false;
        var middleware = new AuthInputSanitizationMiddleware(_ =>
        {
            endpointCalled = true;
            return Task.CompletedTask;
        });
        var ctx = AuthPost("/api/auth/register", """{"email":"user<x>@a.com","password":"p"}""");

        await middleware.InvokeAsync(ctx);

        Assert.False(endpointCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Contains("email", ResponseBody(ctx));
    }

    [Fact]
    public async Task Never_mutates_the_request_body()
    {
        string? bodySeen = null;
        var middleware = new AuthInputSanitizationMiddleware(async c =>
        {
            using var reader = new StreamReader(c.Request.Body);
            bodySeen = await reader.ReadToEndAsync();
        });
        const string body = """{"email":"safe.user+tag@example.com","password":"p@ss word"}""";
        var ctx = AuthPost("/api/auth/register", body);

        await middleware.InvokeAsync(ctx);

        Assert.Equal(body, bodySeen);
    }

    [Fact]
    public async Task Register_error_details_pass_through_unchanged()
    {
        var middleware = new AuthInputSanitizationMiddleware(async c =>
        {
            c.Response.StatusCode = StatusCodes.Status400BadRequest;
            await c.Response.WriteAsync(
                """{"errors":{"PasswordTooShort":["Passwords must be at least 6 characters."]}}""");
        });
        var ctx = AuthPost("/api/auth/register", """{"email":"a@b.com","password":"p"}""");

        await middleware.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Contains("PasswordTooShort", ResponseBody(ctx));
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    public async Task Login_errors_are_normalized_to_a_generic_message(int statusCode)
    {
        var middleware = new AuthInputSanitizationMiddleware(async c =>
        {
            c.Response.StatusCode = statusCode;
            await c.Response.WriteAsync("""{"detail":"LockedOut"}""");
        });
        var ctx = AuthPost("/api/auth/login", """{"email":"a@b.com","password":"wrong"}""");

        await middleware.InvokeAsync(ctx);

        var body = ResponseBody(ctx);
        Assert.Equal(statusCode, ctx.Response.StatusCode);
        Assert.DoesNotContain("LockedOut", body);
        Assert.Contains("Invalid email or password", body);
    }

    [Fact]
    public async Task Login_success_body_passes_through()
    {
        var middleware = new AuthInputSanitizationMiddleware(async c =>
        {
            c.Response.StatusCode = StatusCodes.Status200OK;
            await c.Response.WriteAsync("""{"accessToken":"abc"}""");
        });
        var ctx = AuthPost("/api/auth/login", """{"email":"a@b.com","password":"right"}""");

        await middleware.InvokeAsync(ctx);

        Assert.Contains("accessToken", ResponseBody(ctx));
    }

    private static DefaultHttpContext AuthPost(string path, string json)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = HttpMethods.Post;
        ctx.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static string ResponseBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var reader = new StreamReader(ctx.Response.Body, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
