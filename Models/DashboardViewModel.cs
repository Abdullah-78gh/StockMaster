namespace StockMaster.Models
{
    public class DashboardViewModel
    {
        // Summary Cards
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TodaySales { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingNotifications { get; set; }

        // Charts Data
        public List<ChartData>? SalesTrend { get; set; }
        public List<CategoryDistribution>? CategoryDistribution { get; set; }

        // Recent Activity
        public List<Sale>? RecentSales { get; set; }
        public List<InventoryLog>? RecentInventoryLogs { get; set; }
        public List<LowStockNotification>? LowStockAlerts { get; set; }

        // Top Products
        public List<ProductPerformance>? TopSellingProducts { get; set; }
    }

    public class ChartData
    {
        public string? Label { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }

    public class CategoryDistribution
    {
        public string? CategoryName { get; set; }
        public int ProductCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class ProductPerformance
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}