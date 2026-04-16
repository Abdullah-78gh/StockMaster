using StockMaster.Models;
using StockMaster.Repositories;

namespace StockMaster.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            return await _dashboardRepository.GetDashboardDataAsync();
        }
    }
}