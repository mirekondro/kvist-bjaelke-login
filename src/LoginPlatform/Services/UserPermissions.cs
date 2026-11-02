using System.Security.Claims;
using LoginPlatform.Models;

namespace LoginPlatform.Services;

public static class UserPermissions
{
    // Smí přihlášený uživatel (actor) spravovat uživatele s rolí targetRole?
    public static bool CanManage(ClaimsPrincipal actor, Role targetRole) =>
        actor.IsInRole(nameof(Role.Admin)) ||
        (actor.IsInRole(nameof(Role.SuperUser)) && targetRole == Role.User);

    // Které role smí actor přidělit
    public static IEnumerable<Role> AssignableRoles(ClaimsPrincipal actor) =>
        Enum.GetValues<Role>().Where(r => CanManage(actor, r));

    public static int? CurrentUserId(ClaimsPrincipal actor) =>
        int.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
