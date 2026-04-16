using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public interface IReportService
    {
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<InventoryReportViewModel> GetInventoryReportAsync();
        Task<ProductPerformanceReportViewModel> GetProductPerformanceReportAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateSalesReportExcelAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateInventoryReportExcelAsync();
    }
}