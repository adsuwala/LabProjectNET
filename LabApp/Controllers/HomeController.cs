using System.Diagnostics;
using LabApp.Data;
using LabApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string? search, string? category)
        {
            var query = _db.Products
                .Where(p => p.Published)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null && p.Category.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim();
                query = query.Where(p => p.Category == cat);
            }

            var categories = _db.Products
                .Where(p => p.Published && !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking()
                .ToList();

            var vm = new ProductListViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
                SearchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
            };

            return View(vm);
        }

        [HttpGet("Promotions")]
        public IActionResult Promotions(string? search, string? category)
        {
            var query = _db.Products
                .Where(p => p.Published && p.PromoPrice.HasValue)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null && p.Category.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim();
                query = query.Where(p => p.Category == cat);
            }

            var categories = _db.Products
                .Where(p => p.Published && p.PromoPrice.HasValue && !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking()
                .ToList();

            var vm = new ProductListViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
                SearchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
            };

            return View(vm);
        }

        [HttpGet("Categories")]
        public IActionResult Categories()
        {
            var categories = _db.Products
                .Where(p => p.Published && !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(categories);
        }

        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
