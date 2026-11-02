using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoginPlatform.Pages;

public class IndexModel : PageModel
{
    // Úvodní stránka = seznam uživatelů
    public IActionResult OnGet() => RedirectToPage("/Users/Index");
}
