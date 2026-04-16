using StockMaster.Models;
using StockMaster.Repositories;
using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public class SalesService : ISalesService
    {
        private readonly ISaleRepository _saleRepository;
        private readonly IProductRepository _productRepository;

        public SalesService(
            ISaleRepository saleRepository,
            IProductRepository productRepository)
        {
            _saleRepository = saleRepository;
            _productRepository = productRepository;
        }
        public async Task<Sale> CreateSaleAsync(CreateSaleViewModel model, int userId)
        {
            if (model.CartItems == null || !model.CartItems.Any())
                throw new InvalidOperationException("Cart cannot be empty");

            // Create sale items
            var saleItems = model.CartItems.Select(item => new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                TotalPrice = (item.UnitPrice * item.Quantity) - item.Discount
            }).ToList();

            // Calculate totals
            var subTotal = saleItems.Sum(i => i.UnitPrice * i.Quantity);
            var taxAmount = subTotal * 0.10m;
            var totalDiscount = saleItems.Sum(i => i.Discount);
            var totalAmount = subTotal + taxAmount - totalDiscount;
            var changeAmount = model.AmountPaid > totalAmount ? model.AmountPaid - totalAmount : 0;
            var dueAmount = totalAmount > model.AmountPaid ? totalAmount - model.AmountPaid : 0;

            // Create sale
            var sale = new Sale
            {
                SubTotal = subTotal,
                TaxAmount = taxAmount,
                DiscountAmount = totalDiscount,
                TotalAmount = totalAmount,
                AmountPaid = model.AmountPaid,
                ChangeAmount = changeAmount,
                DueAmount = dueAmount,
                PaymentMethod = model.PaymentMethod,
                Notes = model.Notes ?? string.Empty,
                PaymentStatus = model.AmountPaid >= totalAmount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid,
                SaleDate = DateTime.UtcNow
            };

            return await _saleRepository.CreateSaleAsync(sale, saleItems, userId);
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            return await _saleRepository.GetSaleWithItemsAsync(id);
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _saleRepository.GetSalesByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Sale>> GetRecentSalesAsync(int count)
        {
            return await _saleRepository.GetRecentSalesAsync(count);
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var sales = await GetSalesByDateRangeAsync(startDate, endDate);
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

        public async Task<IEnumerable<Product>> GetAvailableProductsAsync()
        {
            var products = await _productRepository.GetProductsWithCategoryAsync();
            return products.Where(p => p.QuantityInStock > 0);
        }

        public async Task<Sale?> GetSaleByInvoiceNumberAsync(string invoiceNumber)
        {
            var sales = await _saleRepository.FindAsync(s => s.InvoiceNumber == invoiceNumber);
            return sales.FirstOrDefault();
        }

        public async Task<bool> CancelSaleAsync(int saleId, int userId, string reason)
        {
            var sale = await GetSaleByIdAsync(saleId);
            if (sale == null) return false;

            if (sale.PaymentStatus == PaymentStatus.Cancelled)
                throw new InvalidOperationException("Sale is already cancelled");

            foreach (var item in sale.SaleItems)
            {
                await _productRepository.UpdateStockAsync(
                    item.ProductId,
                    item.Quantity,
                    InventoryAction.Return,
                    userId,
                    $"Cancelled sale: {sale.InvoiceNumber}");
            }

            sale.PaymentStatus = PaymentStatus.Cancelled;
            sale.Notes += $" | Cancelled: {reason}";
            _saleRepository.Update(sale);
            await _saleRepository.SaveChangesAsync();

            return true;
        }
    }
}