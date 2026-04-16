using Microsoft.EntityFrameworkCore;
using StockMaster.Data;
using StockMaster.Models;

namespace StockMaster.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetCategoryWithProductsAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId.Value);
            }
            return await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<CategoryDistribution>> GetCategoryDistributionAsync()
        {
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);

            var distribution = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryDistribution
                {
                    CategoryName = c.Name,
                    ProductCount = c.Products.Count(p => p.IsActive),
                    Percentage = totalProducts > 0
                        ? (decimal)c.Products.Count(p => p.IsActive) / totalProducts * 100
                        : 0
                })
                .OrderByDescending(c => c.ProductCount)
                .ToListAsync();

            return distribution;
        }
    }
}