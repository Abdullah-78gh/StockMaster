using Microsoft.AspNetCore.Mvc;
using StockMaster.Services;
using StockMaster.ViewModels;
using StockMaster.Models;
using System.Text.Json;

namespace StockMaster.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly IInventoryService _inventoryService;
        private readonly IAuthService _authService;

        public SalesController(
            ISalesService salesService,
            IInventoryService inventoryService,
            IAuthService authService)
        {
            _salesService = salesService;
            _inventoryService = inventoryService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            if (!startDate.HasValue)
                startDate = DateTime.Today;
            if (!endDate.HasValue)
                endDate = DateTime.Today;

            var sales = await _salesService.GetSalesByDateRangeAsync(startDate.Value, endDate.Value);

            ViewBag.CurrentUser = currentUser;
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(sales);
        }

        [HttpGet]
        public async Task<IActionResult> NewSale()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var products = await _salesService.GetAvailableProductsAsync();
            ViewBag.Products = products;
            ViewBag.CurrentUser = currentUser;

            return View(new CreateSaleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewSale(CreateSaleViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            // Get cart from hidden field
            var cartJson = Request.Form["CartItems"].ToString();

            if (!string.IsNullOrEmpty(cartJson))
            {
                try
                {
                    model.CartItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
                    Console.WriteLine($"Deserialized {model.CartItems?.Count} items");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                    ModelState.AddModelError("", $"Error reading cart: {ex.Message}");
                }
            }

            if (model.CartItems == null || !model.CartItems.Any())
            {
                ModelState.AddModelError("", "Cart cannot be empty");
                ViewBag.Products = await _salesService.GetAvailableProductsAsync();
                ViewBag.CurrentUser = currentUser;
                return View(model);
            }

            // Validate stock
            foreach (var item in model.CartItems)
            {
                var product = await _inventoryService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Product {item.ProductName} not found");
                    ViewBag.Products = await _salesService.GetAvailableProductsAsync();
                    ViewBag.CurrentUser = currentUser;
                    return View(model);
                }

                if (product.QuantityInStock < item.Quantity)
                {
                    ModelState.AddModelError("", $"Insufficient stock for {product.Name}. Available: {product.QuantityInStock}");
                    ViewBag.Products = await _salesService.GetAvailableProductsAsync();
                    ViewBag.CurrentUser = currentUser;
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _salesService.GetAvailableProductsAsync();
                ViewBag.CurrentUser = currentUser;
                return View(model);
            }

            try
            {
                var sale = await _salesService.CreateSaleAsync(model, currentUser.Id);
                TempData["Success"] = $"Sale completed! Invoice: {sale.InvoiceNumber}";
                return RedirectToAction("Receipt", new { id = sale.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }

                ModelState.AddModelError("", $"Error creating sale: {ex.Message}");
                ViewBag.Products = await _salesService.GetAvailableProductsAsync();
                ViewBag.CurrentUser = currentUser;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound();

            ViewBag.CurrentUser = currentUser;
            return View(sale);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound();

            ViewBag.CurrentUser = currentUser;
            return View(sale);
        }
    }
}