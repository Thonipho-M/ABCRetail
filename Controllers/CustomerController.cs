using Microsoft.AspNetCore.Mvc;
using StudentApplication.Services;
using System.Threading.Tasks;

namespace StudentApplication.Controllers
{
    public class CustomerController : Controller
    {
        private readonly RetailStorageService _storage;

        public CustomerController(RetailStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(string id, string name, string email)
        {
            await _storage.AddCustomerAsync(id, name, email);
            ViewBag.Message = "Customer added successfully!";
            return View("Index");
        }
    }
}
