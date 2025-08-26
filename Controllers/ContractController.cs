using Microsoft.AspNetCore.Mvc;
using StudentApplication.Services;
using System.Threading.Tasks;

namespace StudentApplication.Controllers
{
    public class ContractController : Controller
    {
        private readonly RetailStorageService _storage;

        public ContractController(RetailStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _storage.UploadContractAsync(file.FileName, stream);
                ViewBag.Message = "Contract uploaded successfully!";
            }
            return View("Index");
        }
    }
}
