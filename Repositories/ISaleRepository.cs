using StockMaster.Models;

namespace StockMaster.Repositories
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetSaleWithItemsAsync(int id);
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<string> GenerateInvoiceNumberAsync();
        Task<Sale> CreateSaleAsync(Sale sale, List<SaleItem> items, int userId);
        Task<IEnumerable<Sale>> GetRecentSalesAsync(int count);
        Task<decimal> GetTodaySalesTotalAsync();
        Task<int> GetTodaySalesCountAsync();
        Task<IEnumerable<ProductPerformance>> GetTopSellingProductsAsync(int count);
        Task<IEnumerable<ChartData>> GetSalesTrendAsync(int days);
    }
}