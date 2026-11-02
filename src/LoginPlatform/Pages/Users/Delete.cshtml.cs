using LoginPlatform.Data;
using LoginPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserEntity = LoginPlatform.Models.User;

namespace LoginPlatform.Pages.Users;

[Authorize(Roles = "SuperUser,Admin")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public UserEntity? Target { get; private set; }
    public bool IsSelf => UserPermissions.CurrentUserId(User) == Id;

    public async Task<IActionResult> OnGetAsync()
    {
        Target = await _db.Users.FindAsync(Id);
        if (Target is null) return NotFound();
        if (!UserPermissions.CanManage(User, Target.Role)) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var target = await _db.Users.FindAsync(Id);
        if (target is null) return NotFound();
        // Nikdo nesmí smazat sám sebe (admin by se zamkl mimo systém)
        if (!UserPermissions.CanManage(User, target.Role) || IsSelf) return Forbid();

        _db.Users.Remove(target);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
