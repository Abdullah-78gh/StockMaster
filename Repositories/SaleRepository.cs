using Microsoft.EntityFrameworkCore;
using StockMaster.Data;
using StockMaster.Models;

namespace StockMaster.Repositories
{
    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        private readonly IProductRepository _productRepository;

        public SaleRepository(ApplicationDbContext context, IProductRepository productRepository)
            : base(context)
        {
            _productRepository = productRepository;
        }

        public async Task<Sale?> GetSaleWithItemsAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Sales
                .Include(s => s.User)
                .Where(s => s.SaleDate.Date >= startDate.Date && s.SaleDate.Date <= endDate.Date)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var date = DateTime.UtcNow;
            var year = date.Year.ToString();
            var month = date.Month.ToString("D2");
            var day = date.Day.ToString("D2");

            var lastSaleToday = await _context.Sales
                .Where(s => s.SaleDate.Date == date.Date)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastSaleToday != null && !string.IsNullOrEmpty(lastSaleToday.InvoiceNumber))
            {
                var parts = lastSaleToday.InvoiceNumber.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"INV-{year}{month}{day}-{sequence:D4}";
        }

        public async Task<Sale> CreateSaleAsync(Sale sale, List<SaleItem> items, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Console.WriteLine("Starting transaction for sale creation");

                // Generate invoice number
                sale.InvoiceNumber = await GenerateInvoiceNumberAsync();
                sale.SaleDate = DateTime.UtcNow;
                sale.UserId = userId;

                // Ensure all required fields have values
                sale.SubTotal = items.Sum(i => i.UnitPrice * i.Quantity);
                sale.TaxAmount = sale.SubTotal * 0.10m; // Calculate tax if not set
                sale.TotalAmount = sale.SubTotal + sale.TaxAmount - sale.DiscountAmount;
                sale.ChangeAmount = sale.AmountPaid - sale.TotalAmount;
                sale.DueAmount = sale.TotalAmount - sale.AmountPaid;

                if (sale.DueAmount < 0) sale.DueAmount = 0;
                if (sale.ChangeAmount < 0) sale.ChangeAmount = 0;

                // Set PaymentStatus based on AmountPaid
                if (sale.AmountPaid >= sale.TotalAmount)
                {
                    sale.PaymentStatus = PaymentStatus.Paid;
                }
                else if (sale.AmountPaid > 0)
                {
                    sale.PaymentStatus = PaymentStatus.PartiallyPaid;
                }
                else
                {
                    sale.PaymentStatus = PaymentStatus.Pending;
                }

                Console.WriteLine($"Invoice Number: {sale.InvoiceNumber}");
                Console.WriteLine($"SubTotal: {sale.SubTotal}");
                Console.WriteLine($"TaxAmount: {sale.TaxAmount}");
                Console.WriteLine($"DiscountAmount: {sale.DiscountAmount}");
                Console.WriteLine($"TotalAmount: {sale.TotalAmount}");
                Console.WriteLine($"AmountPaid: {sale.AmountPaid}");
                Console.WriteLine($"ChangeAmount: {sale.ChangeAmount}");
                Console.WriteLine($"DueAmount: {sale.DueAmount}");
                Console.WriteLine($"PaymentStatus: {sale.PaymentStatus}");
                Console.WriteLine($"PaymentMethod: {sale.PaymentMethod}");

                // Add sale
                await _context.Sales.AddAsync(sale);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Sale saved with ID: {sale.Id}");

                // Add sale items
                foreach (var item in items)
                {
                    item.SaleId = sale.Id;

                    // Ensure item has all required fields
                    item.TotalPrice = (item.UnitPrice * item.Quantity) - item.Discount;

                    await _context.SaleItems.AddAsync(item);
                    Console.WriteLine($"SaleItem added for ProductId: {item.ProductId}, Quantity: {item.Quantity}, Total: {item.TotalPrice}");

                    // Update product stock
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null)
                        throw new InvalidOperationException($"Product {item.ProductId} not found during stock update");

                    if (product.QuantityInStock < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.QuantityInStock}, Requested: {item.Quantity}");

                    // Update stock
                    await _productRepository.UpdateStockAsync(
                        item.ProductId,
                        item.Quantity,
                        InventoryAction.Sale,
                        userId,
                        sale.InvoiceNumber);
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("All changes saved, committing transaction");

                await transaction.CommitAsync();
                Console.WriteLine("Transaction committed successfully");

                return sale;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateSaleAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                await transaction.RollbackAsync();
                throw new Exception($"Error creating sale: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Sale>> GetRecentSalesAsync(int count)
        {
            return await _context.Sales
                .Include(s => s.User)
                .OrderByDescending(s => s.SaleDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<decimal> GetTodaySalesTotalAsync()
        {
            var today = DateTime.Today;
            var sales = await _context.Sales
                .Where(s => s.SaleDate.Date == today && s.PaymentStatus != PaymentStatus.Cancelled)
                .ToListAsync();
            return sales.Sum(s => s.TotalAmount);
        }

        public async Task<int> GetTodaySalesCountAsync()
        {
            var today = DateTime.Today;
            return await _context.Sales
                .CountAsync(s => s.SaleDate.Date == today && s.PaymentStatus != PaymentStatus.Cancelled);
        }

        public async Task<IEnumerable<ProductPerformance>> GetTopSellingProductsAsync(int count)
        {
            var topProducts = await _context.SaleItems
                .Include(si => si.Product)
                .Where(si => si.Product != null)
                .GroupBy(si => new { si.ProductId, si.Product!.Name })
                .Select(g => new ProductPerformance
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantitySold = g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.TotalPrice)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(count)
                .ToListAsync();

            return topProducts;
        }

        public async Task<IEnumerable<ChartData>> GetSalesTrendAsync(int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var sales = await _context.Sales
                .Where(s => s.SaleDate >= startDate && s.PaymentStatus != PaymentStatus.Cancelled)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new ChartData
                {
                    Date = g.Key,
                    Value = g.Sum(s => s.TotalAmount),
                    Label = g.Key.ToString("MMM dd")
                })
                .OrderBy(c => c.Date)
                .ToListAsync();

            // Fill in missing dates
            var result = new List<ChartData>();
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                var sale = sales.FirstOrDefault(s => s.Date.Date == date.Date);
                result.Add(new ChartData
                {
                    Date = date,
                    Value = sale?.Value ?? 0,
                    Label = date.ToString("MMM dd")
                });
            }

            return result;
        }
    }
}