using Microsoft.EntityFrameworkCore;
using StockMaster.Data;
using StockMaster.Models;

namespace StockMaster.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.QuantityInStock <= p.MinimumStockLevel)
                .OrderBy(p => p.QuantityInStock)
                .ToListAsync();
        }

        public async Task<bool> SKUExistsAsync(string sku, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Products
                    .AnyAsync(p => p.SKU == sku && p.Id != excludeId.Value);
            }
            return await _context.Products.AnyAsync(p => p.SKU == sku);
        }

        public async Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(barcode))
                return false;

            if (excludeId.HasValue)
            {
                return await _context.Products
                    .AnyAsync(p => p.Barcode == barcode && p.Id != excludeId.Value);
            }
            return await _context.Products.AnyAsync(p => p.Barcode == barcode);
        }

        public async Task UpdateStockAsync(int productId, int quantity, InventoryAction action, int userId, string? referenceNumber = null)
        {
            var product = await GetByIdAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            var previousQuantity = product.QuantityInStock;
            var newQuantity = previousQuantity;

            Console.WriteLine($"UpdateStockAsync - Product: {product.Name}, Action: {action}, Quantity: {quantity}, Previous: {previousQuantity}");

            switch (action)
            {
                case InventoryAction.StockIn:
                case InventoryAction.Purchase:
                case InventoryAction.Return:
                    newQuantity = previousQuantity + quantity;
                    Console.WriteLine($"Stock In: {previousQuantity} + {quantity} = {newQuantity}");
                    break;

                case InventoryAction.StockOut:
                case InventoryAction.Sale:
                    if (previousQuantity < quantity)
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {previousQuantity}, Requested: {quantity}");
                    newQuantity = previousQuantity - quantity;
                    Console.WriteLine($"Stock Out/Sale: {previousQuantity} - {quantity} = {newQuantity}");
                    break;

                case InventoryAction.Damaged:
                case InventoryAction.Lost:
                    if (previousQuantity < quantity)
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {previousQuantity}, Requested: {quantity}");
                    newQuantity = previousQuantity - quantity;
                    Console.WriteLine($"Damaged/Lost: {previousQuantity} - {quantity} = {newQuantity}");
                    break;

                case InventoryAction.Adjustment:
                    newQuantity = quantity;
                    Console.WriteLine($"Adjustment: {previousQuantity} -> {newQuantity}");
                    break;

                default:
                    throw new ArgumentException($"Invalid inventory action: {action}");
            }

            // Update product stock
            product.QuantityInStock = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;
            Update(product);

            Console.WriteLine($"Stock updated for {product.Name}: {previousQuantity} -> {newQuantity}");

            // Log the inventory change
            var inventoryLog = new InventoryLog
            {
                ProductId = productId,
                Action = action,
                Quantity = quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                UnitCost = product.Cost,
                PerformedBy = userId,
                ReferenceNumber = referenceNumber,
                Notes = $"{action} - {quantity} units" + (string.IsNullOrEmpty(referenceNumber) ? "" : $" (Ref: {referenceNumber})"),
                CreatedAt = DateTime.UtcNow
            };

            await _context.InventoryLogs.AddAsync(inventoryLog);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Inventory log created for {product.Name}");

            // Check for low stock and create notification
            if (newQuantity <= product.MinimumStockLevel && newQuantity > 0)
            {
                var existingNotification = await _context.LowStockNotifications
                    .FirstOrDefaultAsync(n => n.ProductId == productId && !n.IsResolved);

                if (existingNotification == null)
                {
                    var notification = new LowStockNotification
                    {
                        ProductId = productId,
                        CurrentQuantity = newQuantity,
                        MinimumStockLevel = product.MinimumStockLevel,
                        CreatedAt = DateTime.UtcNow,
                        IsResolved = false
                    };
                    await _context.LowStockNotifications.AddAsync(notification);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Low stock notification created for {product.Name} (Stock: {newQuantity}, Min: {product.MinimumStockLevel})");
                }
                else
                {
                    existingNotification.CurrentQuantity = newQuantity;
                    existingNotification.CreatedAt = DateTime.UtcNow;
                    _context.LowStockNotifications.Update(existingNotification);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Low stock notification updated for {product.Name}");
                }
            }
            else if (newQuantity == 0)
            {
                // Check if notification already exists
                var existingNotification = await _context.LowStockNotifications
                    .FirstOrDefaultAsync(n => n.ProductId == productId && !n.IsResolved);

                if (existingNotification == null)
                {
                    var notification = new LowStockNotification
                    {
                        ProductId = productId,
                        CurrentQuantity = 0,
                        MinimumStockLevel = product.MinimumStockLevel,
                        CreatedAt = DateTime.UtcNow,
                        IsResolved = false
                    };
                    await _context.LowStockNotifications.AddAsync(notification);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Out of stock notification created for {product.Name}");
                }
            }
            else if (newQuantity > product.MinimumStockLevel)
            {
                // If stock is now above minimum, resolve any pending notifications
                var pendingNotifications = await _context.LowStockNotifications
                    .Where(n => n.ProductId == productId && !n.IsResolved)
                    .ToListAsync();

                foreach (var notification in pendingNotifications)
                {
                    notification.IsResolved = true;
                    notification.ResolvedAt = DateTime.UtcNow;
                    notification.ResolvedBy = userId;
                    notification.ResolutionNotes = $"Stock replenished to {newQuantity} units";
                    _context.LowStockNotifications.Update(notification);
                }

                if (pendingNotifications.Any())
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Resolved {pendingNotifications.Count} low stock notifications for {product.Name}");
                }
            }
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetProductsWithCategoryAsync();

            searchTerm = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive &&
                    (p.Name.ToLower().Contains(searchTerm) ||
                     p.SKU.ToLower().Contains(searchTerm) ||
                     (p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm)) ||
                     (p.Description != null && p.Description.ToLower().Contains(searchTerm))))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);
        }

        public async Task<int> GetTotalProductCountAsync()
        {
            return await _context.Products.CountAsync(p => p.IsActive);
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            return products.Sum(p => p.Cost * p.QuantityInStock);
        }
    }
}