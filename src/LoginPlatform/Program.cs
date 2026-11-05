using System.Security.Claims;
using System.Threading.RateLimiting;
using LoginPlatform.Data;
using LoginPlatform.Models;
using LoginPlatform.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToFolder("/Account");
    o.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.Cookie.Name = "kb.auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;

        // On every request the cookie is checked against the database.
        // Without this, a deleted or demoted user would keep their old role
        // until the cookie expires, because the role is stored inside the cookie.
        o.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async ctx =>
            {
                var idValue = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = int.TryParse(idValue, out var id)
                    ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
                    : null;

                if (user is null || ctx.Principal!.FindFirstValue(ClaimTypes.Role) != user.Role.ToString())
                {
                    // User was deleted or their role changed -> log them out
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                else if (ctx.Principal.FindFirstValue(ClaimTypes.Name) != user.Username)
                {
                    // Username was changed -> keep the session but update the cookie
                    ctx.ReplacePrincipal(UserPrincipalFactory.Create(user));
                    ctx.ShouldRenew = true;
                }
            }
        };
    });
builder.Services.AddAuthorization();

// Brute-force protection: max 5 login attempts per minute from one IP address.
// Only POST /Account/Login is limited; the rest of the site is not affected.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        HttpMethods.IsPost(http.Request.Method) &&
        http.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
            ? RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter("not-limited"));
    o.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync(
            "Too many login attempts. Please wait one minute and try again.", token);
    };
});

// Keys used to encrypt the auth cookie. In Docker they are stored in the volume,
// so users are not logged out when the container restarts.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var adminPassword = app.Configuration["SeedAdmin:Password"];
    if (!db.Users.Any() && !string.IsNullOrWhiteSpace(adminPassword))
    {
        db.Users.Add(new User
        {
            Username = app.Configuration["SeedAdmin:Username"] ?? "admin",
            PasswordHash = PasswordService.Hash(adminPassword),
            Role = Role.Admin
        });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
