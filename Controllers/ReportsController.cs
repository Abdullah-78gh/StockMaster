using Microsoft.AspNetCore.Mvc;
using StockMaster.Services;
using StockMaster.ViewModels;

namespace StockMaster.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IAuthService _authService;

        public ReportsController(IReportService reportService, IAuthService authService)
        {
            _reportService = reportService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            if (!startDate.HasValue)
                startDate = DateTime.Today.AddDays(-30);
            if (!endDate.HasValue)
                endDate = DateTime.Today;

            var report = await _reportService.GetSalesReportAsync(startDate.Value, endDate.Value);
            ViewBag.CurrentUser = currentUser;

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var report = await _reportService.GetInventoryReportAsync();
            ViewBag.CurrentUser = currentUser;

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ProductPerformance(DateTime? startDate, DateTime? endDate)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            if (!startDate.HasValue)
                startDate = DateTime.Today.AddDays(-30);
            if (!endDate.HasValue)
                endDate = DateTime.Today;

            var report = await _reportService.GetProductPerformanceReportAsync(startDate.Value, endDate.Value);
            ViewBag.CurrentUser = currentUser;

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportSalesReport(DateTime startDate, DateTime endDate)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var fileContent = await _reportService.GenerateSalesReportExcelAsync(startDate, endDate);
            var fileName = $"Sales_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

            return File(fileContent, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryReport()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var fileContent = await _reportService.GenerateInventoryReportExcelAsync();
            var fileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd}.csv";

            return File(fileContent, "text/csv", fileName);
        }
    }
}