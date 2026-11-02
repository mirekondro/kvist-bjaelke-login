using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoginPlatform.Pages;

public class IndexModel : PageModel
{
    // Home page = list of users
    public IActionResult OnGet() => RedirectToPage("/Users/Index");
}
