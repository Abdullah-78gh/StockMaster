using Microsoft.AspNetCore.Mvc;
using StockMaster.ViewModels;
using System.Text.Json;

namespace StockMaster.Controllers
{
    public class DebugController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> InspectRequest()
        {
            Console.WriteLine("========== DEBUG INSPECT REQUEST ==========");

            // Read the raw request body
            Request.EnableBuffering();
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            Request.Body.Position = 0;

            Console.WriteLine($"Raw Request Body: {body}");

            // Try to deserialize with different options
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var model = JsonSerializer.Deserialize<CreateSaleViewModel>(body, options);

                if (model != null)
                {
                    Console.WriteLine($"Deserialized - PaymentMethod: {model.PaymentMethod}");
                    Console.WriteLine($"Deserialized - AmountPaid: {model.AmountPaid}");
                    Console.WriteLine($"Deserialized - CartItems count: {model.CartItems?.Count ?? 0}");

                    if (model.CartItems != null)
                    {
                        foreach (var item in model.CartItems)
                        {
                            Console.WriteLine($"  Item: {item.ProductId} - {item.ProductName} - Qty: {item.Quantity}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Deserialization returned null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
            }

            return Json(new
            {
                received = body,
                message = "Check console for details"
            });
        }
    }
}