using StockMaster.Models;

namespace StockMaster.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsWithCategoryAsync();
        Task<Product?> GetProductWithCategoryAsync(int id);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<bool> SKUExistsAsync(string sku, int? excludeId = null);
        Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null);
        Task UpdateStockAsync(int productId, int quantity, InventoryAction action, int userId, string? referenceNumber = null);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<Product?> GetProductBySKUAsync(string sku);
        Task<int> GetTotalProductCountAsync();
        Task<decimal> GetTotalInventoryValueAsync();
    }
}