using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using StudentApplication.Services;
using StudentApplication.Models;

namespace StudentApplication.Controllers
{
    public class StudentController : Controller
    {
        private readonly StudentStorageService _studentStorageService;

        public StudentController(StudentStorageService studentStorageService)
        {
            _studentStorageService = studentStorageService;
        }
        public async Task <IActionResult> Index()
        {
            // This action will return the view for displaying student records
            var students = await _studentStorageService.GetStudentsAsync();
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(StudentMark student
            , IFormFile image)
        {

            // Check if the form file is not null and has content
            if(image != null && image.Length > 0)
            {
                //upload the image to blob storage
                var Stream = image.OpenReadStream();
                // Call the service to add the student record
                await _studentStorageService.AddStudentAsync(student, Stream, image.FileName);

                return RedirectToAction(nameof(Index));

                //for error handling for image upload
                ModelState.AddModelError("", "Please upload a valid image file.");
            }

            return View(student);

        }
    }
}
