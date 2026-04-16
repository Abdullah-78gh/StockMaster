using Microsoft.EntityFrameworkCore;
using StockMaster.Data;
using StockMaster.Models;

namespace StockMaster.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly ISaleRepository _saleRepository;

        public DashboardRepository(
            ApplicationDbContext context,
            IProductRepository productRepository,
            ISaleRepository saleRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _saleRepository = saleRepository;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var lowStockProducts = await _productRepository.GetLowStockProductsAsync();
            var lowStockList = lowStockProducts.ToList();

            var outOfStockProducts = await _context.Products
                .CountAsync(p => p.IsActive && p.QuantityInStock == 0);

            var pendingNotifications = await _context.LowStockNotifications
                .CountAsync(n => !n.IsResolved);

            var recentSales = await _saleRepository.GetRecentSalesAsync(10);
            var recentSalesList = recentSales.ToList();

            var recentInventoryLogs = await _context.InventoryLogs
                .Include(l => l.Product)
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            var lowStockAlerts = await _context.LowStockNotifications
                .Include(n => n.Product)
                .Where(n => !n.IsResolved)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var salesTrend = await _saleRepository.GetSalesTrendAsync(7);
            var salesTrendList = salesTrend.ToList();

            var categoryDistribution = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryDistribution
                {
                    CategoryName = c.Name,
                    ProductCount = c.Products.Count(p => p.IsActive)
                })
                .ToListAsync();

            var topProducts = await _saleRepository.GetTopSellingProductsAsync(5);
            var topProductsList = topProducts.ToList();

            var dashboard = new DashboardViewModel
            {
                TotalProducts = await _productRepository.GetTotalProductCountAsync(),
                LowStockProducts = lowStockList.Count,
                OutOfStockProducts = outOfStockProducts,
                TodaySales = await _saleRepository.GetTodaySalesCountAsync(),
                TodayRevenue = await _saleRepository.GetTodaySalesTotalAsync(),
                PendingNotifications = pendingNotifications,

                RecentSales = recentSalesList,
                RecentInventoryLogs = recentInventoryLogs,
                LowStockAlerts = lowStockAlerts,
                SalesTrend = salesTrendList,
                CategoryDistribution = categoryDistribution,
                TopSellingProducts = topProductsList
            };

            return dashboard;
        }
    }
}