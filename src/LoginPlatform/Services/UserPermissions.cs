using System.Security.Claims;
using LoginPlatform.Models;

namespace LoginPlatform.Services;

public static class UserPermissions
{
    // May the signed-in user (actor) manage a user with the role targetRole?
    public static bool CanManage(ClaimsPrincipal actor, Role targetRole) =>
        actor.IsInRole(nameof(Role.Admin)) ||
        (actor.IsInRole(nameof(Role.SuperUser)) && targetRole == Role.User);

    // Roles the actor is allowed to assign
    public static IEnumerable<Role> AssignableRoles(ClaimsPrincipal actor) =>
        Enum.GetValues<Role>().Where(r => CanManage(actor, r));

    public static int? CurrentUserId(ClaimsPrincipal actor) =>
        int.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
