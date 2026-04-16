using Microsoft.EntityFrameworkCore;
using StockMaster.Models;
using StockMaster.Repositories;
using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IRepository<InventoryLog> _inventoryLogRepository;
        private readonly IRepository<LowStockNotification> _notificationRepository;

        public InventoryService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IRepository<InventoryLog> inventoryLogRepository,
            IRepository<LowStockNotification> notificationRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _inventoryLogRepository = inventoryLogRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetProductsWithCategoryAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetProductWithCategoryAsync(id);
        }

        public async Task<Product> CreateProductAsync(ProductViewModel model, int userId)
        {
            // Check if SKU already exists
            if (await _productRepository.SKUExistsAsync(model.SKU))
                throw new InvalidOperationException("SKU already exists");

            // Check if Barcode already exists (if provided)
            if (!string.IsNullOrEmpty(model.Barcode) && await _productRepository.BarcodeExistsAsync(model.Barcode))
                throw new InvalidOperationException("Barcode already exists");

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                SKU = model.SKU,
                Barcode = model.Barcode,
                Price = model.Price,
                Cost = model.Cost,
                QuantityInStock = model.QuantityInStock,
                MinimumStockLevel = model.MinimumStockLevel,
                MaximumStockLevel = model.MaximumStockLevel,
                ReorderPoint = model.ReorderPoint,
                CategoryId = model.CategoryId,
                UnitOfMeasure = model.UnitOfMeasure,
                Weight = model.Weight,
                WeightUnit = model.WeightUnit,
                Location = model.Location,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            // Log initial stock
            if (model.QuantityInStock > 0)
            {
                await _productRepository.UpdateStockAsync(
                    product.Id,
                    model.QuantityInStock,
                    InventoryAction.StockIn,
                    userId,
                    "Initial stock");
            }

            return product;
        }

        public async Task<Product> UpdateProductAsync(ProductViewModel model, int userId)
        {
            var product = await _productRepository.GetByIdAsync(model.Id);
            if (product == null)
                throw new InvalidOperationException("Product not found");

            // Check if SKU already exists (excluding current product)
            if (await _productRepository.SKUExistsAsync(model.SKU, model.Id))
                throw new InvalidOperationException("SKU already exists");

            // Check if Barcode already exists (excluding current product)
            if (!string.IsNullOrEmpty(model.Barcode) &&
                await _productRepository.BarcodeExistsAsync(model.Barcode, model.Id))
                throw new InvalidOperationException("Barcode already exists");

            var oldQuantity = product.QuantityInStock;

            product.Name = model.Name;
            product.Description = model.Description;
            product.SKU = model.SKU;
            product.Barcode = model.Barcode;
            product.Price = model.Price;
            product.Cost = model.Cost;
            product.QuantityInStock = model.QuantityInStock;
            product.MinimumStockLevel = model.MinimumStockLevel;
            product.MaximumStockLevel = model.MaximumStockLevel;
            product.ReorderPoint = model.ReorderPoint;
            product.CategoryId = model.CategoryId;
            product.UnitOfMeasure = model.UnitOfMeasure;
            product.Weight = model.Weight;
            product.WeightUnit = model.WeightUnit;
            product.Location = model.Location;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            // Log stock adjustment if quantity changed
            if (oldQuantity != model.QuantityInStock)
            {
                await _productRepository.UpdateStockAsync(
                    product.Id,
                    model.QuantityInStock,
                    InventoryAction.Adjustment,
                    userId,
                    "Manual adjustment");
            }

            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _productRepository.GetLowStockProductsAsync();
        }

        public async Task<IEnumerable<LowStockNotification>> GetLowStockNotificationsAsync()
        {
            return await _notificationRepository
                .FindAsync(n => !n.IsResolved);
        }

        public async Task ResolveLowStockNotificationAsync(int notificationId, int userId, string? notes = null)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsResolved = true;
                notification.ResolvedAt = DateTime.UtcNow;
                notification.ResolvedBy = userId;
                notification.ResolutionNotes = notes;
                _notificationRepository.Update(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<InventoryLog>> GetInventoryLogsAsync(int productId)
        {
            return await _inventoryLogRepository
                .FindAsync(l => l.ProductId == productId);
        }

        public async Task<IEnumerable<InventoryLog>> GetAllInventoryLogsAsync()
        {
            return await _inventoryLogRepository.GetAllAsync();
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _productRepository.GetProductByBarcodeAsync(barcode);
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            return await _productRepository.GetProductBySKUAsync(sku);
        }

        public async Task<InventoryLog> AdjustStockAsync(int productId, int newQuantity, string reason, int userId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Product not found");

            var oldQuantity = product.QuantityInStock;

            await _productRepository.UpdateStockAsync(
                productId,
                newQuantity,
                InventoryAction.Adjustment,
                userId,
                reason);

            var log = new InventoryLog
            {
                ProductId = productId,
                Action = InventoryAction.Adjustment,
                Quantity = Math.Abs(newQuantity - oldQuantity),
                PreviousQuantity = oldQuantity,
                NewQuantity = newQuantity,
                Notes = reason,
                PerformedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            return log;
        }
    }
}