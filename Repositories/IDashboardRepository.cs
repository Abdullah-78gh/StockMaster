using StockMaster.Models;

namespace StockMaster.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}