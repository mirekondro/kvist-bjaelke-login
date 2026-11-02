using LoginPlatform.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UserEntity = LoginPlatform.Models.User;

namespace LoginPlatform.Pages.Users;

// List of users - available to every signed-in role (User, SuperUser, Admin)
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<UserEntity> UserList { get; private set; } = new();

    public async Task OnGetAsync() =>
        UserList = await _db.Users.OrderBy(u => u.Username).ToListAsync();
}
