using System.ComponentModel.DataAnnotations;
using LoginPlatform.Data;
using LoginPlatform.Models;
using LoginPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginPlatform.Pages.Users;

[Authorize(Roles = "SuperUser,Admin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty, Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    // Nepovinné – když zůstane prázdné, heslo se nemění
    [BindProperty, StringLength(100, MinimumLength = 8)]
    public string? NewPassword { get; set; }

    [BindProperty]
    public Role SelectedRole { get; set; }

    public bool IsSelf => UserPermissions.CurrentUserId(User) == Id;

    public List<SelectListItem> RoleOptions =>
        UserPermissions.AssignableRoles(User)
            .Select(r => new SelectListItem(r.ToString(), r.ToString())).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        var target = await _db.Users.FindAsync(Id);
        if (target is null) return NotFound();
        if (!UserPermissions.CanManage(User, target.Role)) return Forbid();

        Username = target.Username;
        SelectedRole = target.Role;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var target = await _db.Users.FindAsync(Id);
        if (target is null) return NotFound();

        // Musí smět spravovat současnou roli I novou roli (jinak by SuperUser mohl povýšit Usera na Admina)
        if (!UserPermissions.CanManage(User, target.Role) || !UserPermissions.CanManage(User, SelectedRole))
            return Forbid();

        if (IsSelf && SelectedRole != target.Role)
            ModelState.AddModelError(nameof(SelectedRole), "Nemůžete změnit vlastní roli.");

        if (await _db.Users.AnyAsync(u => u.Username == Username && u.Id != Id))
            ModelState.AddModelError(nameof(Username), "Uživatelské jméno už existuje.");

        if (!ModelState.IsValid) return Page();

        target.Username = Username;
        target.Role = SelectedRole;
        if (!string.IsNullOrEmpty(NewPassword))
            target.PasswordHash = PasswordService.Hash(NewPassword);

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
