using System.ComponentModel.DataAnnotations;
using LoginPlatform.Data;
using LoginPlatform.Models;
using LoginPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UserEntity = LoginPlatform.Models.User;

namespace LoginPlatform.Pages.Users;

[Authorize(Roles = "SuperUser,Admin")]   // the User role has no access here
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty, Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public Role SelectedRole { get; set; } = Role.User;

    public List<SelectListItem> RoleOptions =>
        UserPermissions.AssignableRoles(User)
            .Select(r => new SelectListItem(r.ToString(), r.ToString())).ToList();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Server-side check: a SuperUser cannot create an Admin, even by tampering with the form value
        if (!UserPermissions.CanManage(User, SelectedRole))
            return Forbid();

        if (await _db.Users.AnyAsync(u => u.Username == Username))
            ModelState.AddModelError(nameof(Username), "Username already exists.");

        if (!ModelState.IsValid)
            return Page();

        _db.Users.Add(new UserEntity
        {
            Username = Username,
            PasswordHash = PasswordService.Hash(Password),
            Role = SelectedRole
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
