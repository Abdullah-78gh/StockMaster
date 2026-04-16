using StockMaster.Models;
using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public interface IInventoryService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(ProductViewModel model, int userId);
        Task<Product> UpdateProductAsync(ProductViewModel model, int userId);
        Task<bool> DeleteProductAsync(int id);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<IEnumerable<LowStockNotification>> GetLowStockNotificationsAsync();
        Task ResolveLowStockNotificationAsync(int notificationId, int userId, string? notes = null);
        Task<IEnumerable<InventoryLog>> GetInventoryLogsAsync(int productId);
        Task<IEnumerable<InventoryLog>> GetAllInventoryLogsAsync();
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<Product?> GetProductBySKUAsync(string sku);
        Task<InventoryLog> AdjustStockAsync(int productId, int newQuantity, string reason, int userId);
    }
}