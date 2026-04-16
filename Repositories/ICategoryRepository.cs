using StockMaster.Models;

namespace StockMaster.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetCategoryWithProductsAsync(int id);
        Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null);
        Task<IEnumerable<CategoryDistribution>> GetCategoryDistributionAsync();
    }
}