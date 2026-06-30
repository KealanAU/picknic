using Microsoft.AspNetCore.Identity;

namespace Picknic.Api.Models;

/// <summary>Host account. Guests are not users — see Auth/GuestTokenService.</summary>
public class AppUser : IdentityUser
{
}
