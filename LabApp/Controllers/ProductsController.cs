using LabApp.Data;
using LabApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Controllers
{
    [Authorize(Roles = "Admin,Owner")]
    [Route("admin/products")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(products);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ValidatePrices(product);
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.CreatedAt = DateTime.UtcNow;
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [Route("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return BadRequest();

            ValidatePrices(product);
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Category = product.Category;
            existing.Price = product.Price;
            existing.PromoPrice = product.PromoPrice;
            existing.Stock = product.Stock;
            existing.Published = product.Published;

            await _db.SaveChangesAsync();
            await _db.Entry(existing).ReloadAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void ValidatePrices(Product product)
        {
            if (product.PromoPrice.HasValue)
            {
                if (product.PromoPrice <= 0)
                {
                    ModelState.AddModelError(nameof(Product.PromoPrice), "Cena promocyjna musi być większa od zera.");
                }
                else if (product.PromoPrice >= product.Price)
                {
                    ModelState.AddModelError(nameof(Product.PromoPrice), "Cena promocyjna musi być niższa od podstawowej.");
                }
            }
        }
    }
}
