using StockMaster.Models;
using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public interface ISalesService
    {
        Task<Sale> CreateSaleAsync(CreateSaleViewModel model, int userId);
        Task<Sale?> GetSaleByIdAsync(int id);
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Sale>> GetRecentSalesAsync(int count);
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Product>> GetAvailableProductsAsync();
        Task<Sale?> GetSaleByInvoiceNumberAsync(string invoiceNumber);
        Task<bool> CancelSaleAsync(int saleId, int userId, string reason);
    }
}