using LabApp.Data;
using LabApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    [Route("admin/orders")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const int PageSize = 2;

        public AdminOrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            if (page < 1) page = 1;

            var query = _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o =>
                    o.Email.Contains(search) ||
                    o.PublicId.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var orders = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var model = new AdminOrdersViewModel
            {
                Orders = orders,
                SearchTerm = search,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }
    }
}
