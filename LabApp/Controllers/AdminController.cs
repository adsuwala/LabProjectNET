using LabApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private const string OwnerRole = "Owner";
        private const string AdminRole = "Admin";
        private const string UserRole = "User";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("")]
        [HttpGet("users")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<AdminUserListItem>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var displayRole = roles.Contains(OwnerRole)
                    ? OwnerRole
                    : roles.Contains(AdminRole)
                        ? AdminRole
                        : roles.Contains(UserRole)
                            ? UserRole
                            : string.Join(", ", roles);
                var hasUserRole = roles.Contains(UserRole);
                if (string.IsNullOrWhiteSpace(displayRole))
                {
                    displayRole = UserRole;
                    hasUserRole = true;
                }
                model.Add(new AdminUserListItem
                {
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    Id = user.Id,
                    Roles = string.Join(", ", roles),
                    DisplayRole = displayRole,
                    IsAdmin = roles.Contains(AdminRole),
                    IsOwner = roles.Contains(OwnerRole),
                    HasUser = hasUserRole
                });
            }

            return View(model.OrderBy(u => u.Email).ToList());
        }

        [HttpPost]
        [Route("users/make-admin")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = OwnerRole)]
        public async Task<IActionResult> MakeAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.AddToRoleAsync(user, AdminRole);
            await EnsureUserRole(user);
            await RefreshTargetSignIn(user, forceSignOutIfCurrent: false);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("users/remove-admin")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = OwnerRole)]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, OwnerRole))
            {
                return Forbid();
            }

            await _userManager.RemoveFromRoleAsync(user, AdminRole);
            await EnsureUserRole(user);
            await RefreshTargetSignIn(user, forceSignOutIfCurrent: true);
            return RedirectToAction(nameof(Index));
        }

        private async Task EnsureUserRole(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, UserRole))
            {
                await _userManager.AddToRoleAsync(user, UserRole);
            }
        }

        private async Task RefreshTargetSignIn(ApplicationUser user, bool forceSignOutIfCurrent)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var currentId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(currentId) && currentId == user.Id)
                {
                    if (forceSignOutIfCurrent)
                    {
                        await _signInManager.SignOutAsync();
                    }
                    else
                    {
                        await _signInManager.RefreshSignInAsync(user);
                    }
                    return;
                }
            }

            await _userManager.UpdateSecurityStampAsync(user);
        }
    }

    public class AdminUserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public string DisplayRole { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsOwner { get; set; }
        public bool HasUser { get; set; }
    }
}
