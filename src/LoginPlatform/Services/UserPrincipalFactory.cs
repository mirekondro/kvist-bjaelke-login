using System.Security.Claims;
using LoginPlatform.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LoginPlatform.Services;

public static class UserPrincipalFactory
{
    // Builds the identity that is stored (encrypted) in the auth cookie
    public static ClaimsPrincipal Create(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
