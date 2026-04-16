using Microsoft.AspNetCore.Mvc;
using StockMaster.ViewModels;

namespace StockMaster.Controllers
{
    public class TestController : Controller
    {
        [HttpPost]
        public IActionResult TestBinding([FromBody] List<CartItemViewModel> items)
        {
            return Json(new { received = items.Count, items = items });
        }
    }
}