using Microsoft.EntityFrameworkCore;
using StockMaster.Data;
using StockMaster.Models;
using StockMaster.Repositories;
using StockMaster.ViewModels;
using System.Globalization;
using System.Text;

namespace StockMaster.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISaleRepository _saleRepository;
        private readonly IProductRepository _productRepository;

        public ReportService(
            ApplicationDbContext context,
            ISaleRepository saleRepository,
            IProductRepository productRepository)
        {
            _context = context;
            _saleRepository = saleRepository;
            _productRepository = productRepository;
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var sales = await _saleRepository.GetSalesByDateRangeAsync(startDate, endDate);
            var salesList = sales.ToList();

            return new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalTransactions = salesList.Count,
                TotalSales = salesList.Sum(s => s.TotalAmount),
                TotalTax = salesList.Sum(s => s.TaxAmount),
                TotalDiscount = salesList.Sum(s => s.DiscountAmount),
                Sales = salesList
            };
        }

        public async Task<InventoryReportViewModel> GetInventoryReportAsync()
        {
            var products = await _productRepository.GetProductsWithCategoryAsync();
            var productsList = products.ToList();

            return new InventoryReportViewModel
            {
                TotalProducts = productsList.Count,
                TotalCategories = await _context.Categories.CountAsync(c => c.IsActive),
                LowStockItems = productsList.Count(p => p.QuantityInStock <= p.MinimumStockLevel && p.QuantityInStock > 0),
                OutOfStockItems = productsList.Count(p => p.QuantityInStock == 0),
                TotalInventoryValue = productsList.Sum(p => p.Cost * p.QuantityInStock),
                Products = productsList
            };
        }

        public async Task<ProductPerformanceReportViewModel> GetProductPerformanceReportAsync(DateTime startDate, DateTime endDate)
        {
            var saleItems = await _context.SaleItems
                .Include(si => si.Product)
                    .ThenInclude(p => p != null ? p.Category : null)
                .Include(si => si.Sale)
                .Where(si => si.Sale != null && si.Sale.SaleDate >= startDate && si.Sale.SaleDate <= endDate)
                .ToListAsync();

            var productPerformance = saleItems
                .GroupBy(si => new { si.ProductId, si.Product!.Name, CategoryName = si.Product.Category != null ? si.Product.Category.Name : "Uncategorized" })
                .Select(g => new ProductPerformanceDetail
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.TotalPrice),
                    AveragePrice = g.Average(si => si.UnitPrice)
                })
                .OrderByDescending(p => p.QuantitySold)
                .ToList();

            return new ProductPerformanceReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ProductPerformance = productPerformance
            };
        }

        public async Task<byte[]> GenerateSalesReportExcelAsync(DateTime startDate, DateTime endDate)
        {
            var sales = await GetSalesReportAsync(startDate, endDate);

            var sb = new StringBuilder();
            sb.AppendLine("Sales Report");
            sb.AppendLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("Invoice Number,Date,Cashier,Sub Total,Tax,Discount,Total,Payment Method");

            foreach (var sale in sales.Sales)
            {
                sb.AppendLine($"\"{sale.InvoiceNumber}\",{sale.SaleDate:yyyy-MM-dd HH:mm},\"{sale.User?.FullName}\",{sale.SubTotal:F2},{sale.TaxAmount:F2},{sale.DiscountAmount:F2},{sale.TotalAmount:F2},{sale.PaymentMethod}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total Transactions:,{sales.TotalTransactions}");
            sb.AppendLine($"Total Sales:,{sales.TotalSales:F2}");
            sb.AppendLine($"Total Tax:,{sales.TotalTax:F2}");
            sb.AppendLine($"Total Discount:,{sales.TotalDiscount:F2}");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerateInventoryReportExcelAsync()
        {
            var inventory = await GetInventoryReportAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Inventory Report");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine("SKU,Name,Category,Price,Cost,Stock,Minimum Stock,Value");

            foreach (var product in inventory.Products)
            {
                var stockStatus = product.QuantityInStock <= product.MinimumStockLevel ? "Low Stock" : "OK";
                sb.AppendLine($"\"{product.SKU}\",\"{product.Name}\",\"{product.Category?.Name}\",{product.Price:F2},{product.Cost:F2},{product.QuantityInStock},{product.MinimumStockLevel},{product.Cost * product.QuantityInStock:F2}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total Products:,{inventory.TotalProducts}");
            sb.AppendLine($"Total Categories:,{inventory.TotalCategories}");
            sb.AppendLine($"Low Stock Items:,{inventory.LowStockItems}");
            sb.AppendLine($"Out of Stock Items:,{inventory.OutOfStockItems}");
            sb.AppendLine($"Total Inventory Value:,{inventory.TotalInventoryValue:F2}");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}