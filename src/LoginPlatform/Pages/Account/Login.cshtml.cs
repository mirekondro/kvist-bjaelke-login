using LoginPlatform.Data;
using LoginPlatform.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LoginPlatform.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    public LoginModel(AppDbContext db) => _db = db;

    private static readonly string DummyHash = PasswordService.Hash("dummy-password");

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == Username);

        var valid = PasswordService.Verify(Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !valid)
        {
            Error = "Invalid username or password.";
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            UserPrincipalFactory.Create(user));

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return RedirectToPage("/Index");
    }
}
