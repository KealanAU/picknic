using Microsoft.AspNetCore.Identity;
using Picknic.Api.Models;

namespace Picknic.Api.Email;

/// <summary>
/// Bridges ASP.NET Identity's account emails (confirm / password reset) to our
/// <see cref="IEmailSender"/>, so host account flows go through the same provider
/// (ACS in prod, logging in dev) instead of Identity's default no-op sender.
/// </summary>
public class IdentityEmailSender(IEmailSender sender) : IEmailSender<AppUser>
{
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) =>
        sender.SendAsync(email, "Confirm your Picknic account",
            $"<p>Confirm your account by <a href='{confirmationLink}'>tapping here</a>.</p>");

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) =>
        sender.SendAsync(email, "Reset your Picknic password",
            $"<p>Reset your password by <a href='{resetLink}'>tapping here</a>.</p>");

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) =>
        sender.SendAsync(email, "Reset your Picknic password",
            $"<p>Your password reset code is <strong>{resetCode}</strong>.</p>");
}
