using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockMaster.Models;
using StockMaster.Repositories;
using StockMaster.Services;
using StockMaster.ViewModels;

namespace StockMaster.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IAuthService _authService;
        private readonly ICategoryRepository _categoryRepository;

        public ProductsController(
            IInventoryService inventoryService,
            IAuthService authService,
            ICategoryRepository categoryRepository)
        {
            _inventoryService = inventoryService;
            _authService = authService;
            _categoryRepository = categoryRepository;
        }

        private bool IsAdminOrManager()
        {
            var user = _authService.GetCurrentUser();
            return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Manager);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = "", int categoryId = 0)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            IEnumerable<Product> products;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                products = await _inventoryService.GetAllProductsAsync();
                products = products.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.SKU.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (p.Barcode != null && p.Barcode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }
            else if (categoryId > 0)
            {
                products = await _inventoryService.GetAllProductsAsync();
                products = products.Where(p => p.CategoryId == categoryId);
            }
            else
            {
                products = await _inventoryService.GetAllProductsAsync();
            }

            ViewBag.CurrentUser = currentUser;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", categoryId);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdminOrManager())
                return RedirectToAction("AccessDenied", "Account");

            await PopulateCategoriesDropdown();
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!IsAdminOrManager())
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }

            try
            {
                var currentUser = _authService.GetCurrentUser();
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var product = await _inventoryService.CreateProductAsync(model, currentUser.Id);
                TempData["Success"] = $"Product '{product.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdminOrManager())
                return RedirectToAction("AccessDenied", "Account");

            var product = await _inventoryService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Barcode = product.Barcode,
                Price = product.Price,
                Cost = product.Cost,
                QuantityInStock = product.QuantityInStock,
                MinimumStockLevel = product.MinimumStockLevel,
                MaximumStockLevel = product.MaximumStockLevel,
                ReorderPoint = product.ReorderPoint,
                CategoryId = product.CategoryId,
                UnitOfMeasure = product.UnitOfMeasure,
                Weight = product.Weight,
                WeightUnit = product.WeightUnit,
                Location = product.Location,
                IsActive = product.IsActive
            };

            await PopulateCategoriesDropdown(model.CategoryId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!IsAdminOrManager())
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }

            try
            {
                var currentUser = _authService.GetCurrentUser();
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var product = await _inventoryService.UpdateProductAsync(model, currentUser.Id);
                TempData["Success"] = $"Product '{product.Name}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateCategoriesDropdown(model.CategoryId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminOrManager())
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _inventoryService.DeleteProductAsync(id);
            if (result)
            {
                return Json(new { success = true, message = "Product deleted successfully" });
            }

            return Json(new { success = false, message = "Product not found" });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var product = await _inventoryService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var inventoryLogs = await _inventoryService.GetInventoryLogsAsync(id);
            ViewBag.InventoryLogs = inventoryLogs.OrderByDescending(l => l.CreatedAt);
            ViewBag.CurrentUser = currentUser;

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();
            var notifications = await _inventoryService.GetLowStockNotificationsAsync();

            ViewBag.Notifications = notifications;
            ViewBag.CurrentUser = currentUser;

            return View(lowStockProducts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveNotification(int id)
        {
            if (!IsAdminOrManager())
                return Json(new { success = false, message = "Unauthorized" });

            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return Json(new { success = false, message = "User not found" });

            await _inventoryService.ResolveLowStockNotificationAsync(id, currentUser.Id, "Resolved");
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> InventoryLogs()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var logs = await _inventoryService.GetAllInventoryLogsAsync();
            ViewBag.CurrentUser = currentUser;

            return View(logs.OrderByDescending(l => l.CreatedAt));
        }

        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            var product = await _inventoryService.GetProductByBarcodeAsync(barcode);
            if (product == null)
                return NotFound();

            return Json(new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                stock = product.QuantityInStock,
                sku = product.SKU
            });
        }

        private async Task PopulateCategoriesDropdown(object selectedCategory = null)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategory);
        }
    }
}