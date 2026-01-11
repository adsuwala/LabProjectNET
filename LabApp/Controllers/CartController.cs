using System.Globalization;
using System.Linq;
using LabApp.Data;
using LabApp.Helpers;
using LabApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApp.Controllers
{
    [Route("cart")]
    public class CartController : Controller
    {
        private const string CartKey = "CartItems";
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            if (cart.Any())
            {
                var ids = cart.Select(c => c.ProductId).ToList();
                var stocks = await _db.Products.AsNoTracking()
                    .Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Stock);

                foreach (var item in cart)
                {
                    item.AvailableStock = stocks.TryGetValue(item.ProductId, out var stock) ? stock : 0;
                }
                SaveCart(cart);
            }
            var vm = new CartPageViewModel
            {
                Items = cart,
                Checkout = new CheckoutInputModel()
            };

            if (TempData.TryGetValue("CartWarning", out var warning))
            {
                ViewBag.CartWarning = warning;
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    vm.EmailReadOnly = true;
                    vm.Checkout.Email = user.Email ?? string.Empty;
                    vm.Checkout.FullName = user.FullName ?? string.Empty;
                    vm.Checkout.Street = user.Street ?? string.Empty;
                    vm.Checkout.PostalCode = user.PostalCode ?? string.Empty;
                    vm.Checkout.City = user.City ?? string.Empty;
                    vm.Checkout.Phone = user.PhoneNumber ?? string.Empty;
                }
            }

            return View(vm);
        }

        [HttpPost("add/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null || !product.Published)
            {
                return NotFound();
            }
            if (product.Stock <= 0)
            {
                TempData["CartWarning"] = $"Produkt {product.Name} nie jest dostepny.";
                return RedirectToAction(nameof(Index));
            }

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == id);
            var currentQty = existing?.Quantity ?? 0;
            var maxCanAdd = product.Stock - currentQty;
            if (maxCanAdd <= 0)
            {
                TempData["CartWarning"] = $"W koszyku masz maksymalna dostepna ilosc produktu {product.Name}.";
                SaveCart(cart);
                return RedirectToAction(nameof(Index));
            }

            var targetQty = currentQty + quantity;
            string? warning = null;
            if (targetQty > product.Stock)
            {
                targetQty = product.Stock;
                warning = $"Dostepnych jest tylko {product.Stock} sztuk {product.Name}. Ilosc w koszyku ustawiono na maksymalna wartosc.";
            }
            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = id,
                    Name = product.Name,
                    Price = product.Price,
                    PromoPrice = product.PromoPrice,
                    Quantity = targetQty,
                    AvailableStock = product.Stock
                });
            }
            else
            {
                existing.Quantity = targetQty;
                existing.AvailableStock = product.Stock;
            }

            SaveCart(cart);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                TempData["CartWarning"] = warning;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("update/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == id);
            if (existing == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (product == null || !product.Published || product.Stock <= 0)
            {
                cart.Remove(existing);
                TempData["CartWarning"] = $"Produkt {existing.Name} nie jest juz dostepny i zostal usuniety z koszyka.";
                SaveCart(cart);
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                cart.Remove(existing);
            }
            else
            {
                var clampedQty = Math.Min(quantity, product.Stock);
                if (clampedQty < quantity)
                {
                    TempData["CartWarning"] = $"Ilosc produktu {product.Name} zostala zmniejszona do {clampedQty} (tyle na stanie).";
                }
                existing.Quantity = clampedQty;
                existing.AvailableStock = product.Stock;
            }

            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("remove/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == id);
            if (existing != null)
            {
                cart.Remove(existing);
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([Bind(Prefix = "Checkout")] CheckoutInputModel checkout)
        {
            ApplicationUser? user = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    checkout.Email = user.Email ?? string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(checkout.PostalCode))
            {
                var digits = new string(checkout.PostalCode.Where(char.IsDigit).ToArray());
                checkout.PostalCode = digits.Length == 5
                    ? $"{digits.Substring(0, 2)}-{digits.Substring(2, 3)}"
                    : digits;
            }

            if (!string.IsNullOrWhiteSpace(checkout.Phone))
            {
                var digits = new string(checkout.Phone.Where(char.IsDigit).ToArray());
                checkout.Phone = digits;
            }

            ModelState.Clear();
            TryValidateModel(checkout, "Checkout");

            if (!checkout.AcceptTerms)
            {
                ModelState.AddModelError("Checkout.AcceptTerms", "Aby zlozyc zamowienie, zaakceptuj regulamin.");
            }

            if (checkout.CreateAccount && user == null)
            {
                if (string.IsNullOrWhiteSpace(checkout.Password))
                {
                    ModelState.AddModelError("Checkout.Password", "Podaj haslo, aby zalozyc konto.");
                }
                if (string.IsNullOrWhiteSpace(checkout.ConfirmPassword) || checkout.Password != checkout.ConfirmPassword)
                {
                    ModelState.AddModelError("Checkout.ConfirmPassword", "Hasla musza byc takie same.");
                }
                var emailUser = await _userManager.FindByEmailAsync(checkout.Email);
                if (emailUser != null)
                {
                    ModelState.AddModelError("Checkout.Email", "Konto z tym adresem juz istnieje. Zaloguj sie.");
                }
            }

            var cart = GetCart();
            if (!cart.Any())
            {
                ModelState.AddModelError(string.Empty, "Koszyk jest pusty - dodaj produkty zanim zlozysz zamowienie.");
            }

            var productIds = cart.Select(c => c.ProductId).ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            var warnings = new List<string>();
            var cartChanged = false;
            foreach (var item in cart.ToList())
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || !product.Published || product.Stock <= 0)
                {
                    cart.Remove(item);
                    cartChanged = true;
                    warnings.Add($"Produkt {item.Name} nie jest juz dostepny i zostal usuniety z koszyka.");
                    continue;
                }

                if (product.Stock < item.Quantity)
                {
                    item.Quantity = product.Stock;
                    cartChanged = true;
                    warnings.Add($"Ilosc produktu {product.Name} zostala zmniejszona do {product.Stock} (tyle na stanie).");
                }
            }

            if (cartChanged)
            {
                SaveCart(cart);
                var message = string.Join(" ", warnings);
                if (!cart.Any())
                {
                    message = string.IsNullOrWhiteSpace(message)
                        ? "Koszyk jest teraz pusty."
                        : $"{message} Koszyk jest teraz pusty.";
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    TempData["CartWarning"] = message;
                }
                return RedirectToAction(nameof(Index));
            }

            foreach (var item in cart)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError(string.Empty, $"Produkt {item.Name} nie jest juz dostepny.");
                    continue;
                }
                if (!product.Published)
                {
                    ModelState.AddModelError(string.Empty, $"Produkt {product.Name} nie jest juz opublikowany.");
                }

                if (product.Stock < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Produkt {product.Name} ma tylko {product.Stock} sztuk.");
                }
            }

            if (!ModelState.IsValid)
            {
                var vm = new CartPageViewModel
                {
                    Items = cart,
                    Checkout = checkout,
                    EmailReadOnly = user != null
                };
                if (user != null)
                {
                    checkout.Email = user.Email ?? checkout.Email;
                }
                return View("Index", vm);
            }

            if (checkout.CreateAccount && user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = checkout.Email,
                    Email = checkout.Email,
                    FullName = checkout.FullName,
                    Street = checkout.Street,
                    PostalCode = checkout.PostalCode,
                    City = checkout.City,
                    PhoneNumber = checkout.Phone
                };
                var createResult = await _userManager.CreateAsync(newUser, checkout.Password!);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    var vm = new CartPageViewModel
                    {
                        Items = cart,
                        Checkout = checkout,
                        EmailReadOnly = false
                    };
                    return View("Index", vm);
                }
                user = newUser;
                await _signInManager.SignInAsync(newUser, isPersistent: false);
            }

            foreach (var item in cart)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.Stock -= item.Quantity;
            }

            var total = cart.Sum(i => i.Total);

            var order = new Order
            {
                UserId = user?.Id,
                Email = checkout.Email,
                FullName = checkout.FullName,
                Phone = checkout.Phone,
                Street = checkout.Street,
                PostalCode = checkout.PostalCode,
                City = checkout.City,
                Total = total,
                CreatedAt = DateTime.UtcNow,
                Items = cart.Select(c =>
                {
                    var product = products.First(p => p.Id == c.ProductId);
                    return new OrderItem
                    {
                        ProductId = c.ProductId,
                        ProductName = product.Name,
                        UnitPrice = c.Price,
                        PromoPrice = c.PromoPrice,
                        Quantity = c.Quantity,
                        LineTotal = c.Total
                    };
                }).ToList()
            };

            order.PublicId = await GeneratePublicIdAsync();
            _db.Orders.Add(order);

            await _db.SaveChangesAsync();

            if (user != null)
            {
                bool updated = false;
                if (!string.Equals(user.FullName, checkout.FullName, StringComparison.Ordinal))
                {
                    user.FullName = checkout.FullName;
                    updated = true;
                }
                if (!string.Equals(user.Street, checkout.Street, StringComparison.Ordinal))
                {
                    user.Street = checkout.Street;
                    updated = true;
                }
                if (!string.Equals(user.PostalCode, checkout.PostalCode, StringComparison.Ordinal))
                {
                    user.PostalCode = checkout.PostalCode;
                    updated = true;
                }
                if (!string.Equals(user.City, checkout.City, StringComparison.Ordinal))
                {
                    user.City = checkout.City;
                    updated = true;
                }
                if (!string.Equals(user.PhoneNumber, checkout.Phone, StringComparison.Ordinal))
                {
                    user.PhoneNumber = checkout.Phone;
                    updated = true;
                }

                if (updated)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            SaveCart(new List<CartItem>());
            TempData["CheckoutSuccess"] = $"Dziekujemy {checkout.FullName}! Zamowienie zostalo przyjete.";
            TempData["OrderTotal"] = total.ToString("C", CultureInfo.CurrentCulture);
            TempData["OrderNumber"] = order.PublicId;
            return RedirectToAction(nameof(OrderReceived));
        }

        [HttpGet("order-received")]
        public IActionResult OrderReceived()
        {
            if (TempData["CheckoutSuccess"] is not string message)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CheckoutSuccess = message;
            ViewBag.OrderTotal = TempData["OrderTotal"];
            ViewBag.OrderNumber = TempData["OrderNumber"];
            return View();
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CartKey) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);
        }

        private async Task<string> GeneratePublicIdAsync()
        {
            string id;
            do
            {
                var guid = Guid.NewGuid().ToString("N").ToUpperInvariant();
                id = $"ORD-{guid[..10]}";
            } while (await _db.Orders.AnyAsync(o => o.PublicId == id));
            return id;
        }
    }
}
