using Microsoft.AspNetCore.Identity;

namespace Picknic.Api.Models;

/// <summary>
/// Host account. Guests are NOT users — they get a scoped capability token
/// (see Auth/GuestTokenService) and never create an AppUser.
/// </summary>
public class AppUser : IdentityUser
{
}
