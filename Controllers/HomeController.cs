using Microsoft.AspNetCore.Mvc;
using StockMaster.Services;
using StockMaster.Models;

namespace StockMaster.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IDashboardService _dashboardService;

        public HomeController(
            IAuthService authService,
            IDashboardService dashboardService)
        {
            _authService = authService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            ViewBag.CurrentUser = currentUser;

            var dashboardData = await _dashboardService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}