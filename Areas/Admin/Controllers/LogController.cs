using ABCRetail_Project1.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail_Project1.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LogController : Controller
    {
        private readonly AzureStorage _storage;

        public LogController(AzureStorage storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var files = await _storage.ListFilesAsync();
            return View(files);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                await _storage.UploadFileAsync(file);
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Please select a file to upload.");
            return View();
        }

        public async Task<IActionResult> Download(string fileName)
        {
            var fileStream = await _storage.DownloadFileAsync(fileName);
            if (fileStream == null) return NotFound();

            return File(fileStream, "application/octet-stream", fileName);
        }

        
        public async Task<IActionResult> Delete(string fileName)
        {
            await _storage.DeleteFileAsync(fileName);
            return RedirectToAction(nameof(Index));
        }
    }
}
