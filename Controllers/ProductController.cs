using Microsoft.AspNetCore.Mvc;
using StudentApplication.Services;
using System.Threading.Tasks;

namespace StudentApplication.Controllers
{
    public class ProductController : Controller
    {
        private readonly RetailStorageService _storage;

        public ProductController(RetailStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(string id, string name, double price)
        {
            await _storage.AddProductAsync(id, name, price);
            ViewBag.Message = "Product added successfully!";
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _storage.UploadImageAsync(file.FileName, stream);
                ViewBag.ImageUrl = _storage.GetImageUrl(file.FileName);
            }
            return View("Index");
        }
    }
}
