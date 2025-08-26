using Microsoft.AspNetCore.Mvc;
using StudentApplication.Services;
using System.Threading.Tasks;

namespace StudentApplication.Controllers
{
    public class OrderController : Controller
    {
        private readonly RetailStorageService _storage;

        public OrderController(RetailStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            string message = $"Processing order: {orderId}";
            await _storage.AddOrderMessageAsync(message);
            ViewBag.Message = "Order message sent to queue!";
            return View("Index");
        }
    }
}
