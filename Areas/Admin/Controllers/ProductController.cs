using ABCRetail_Project1.Models;
using ABCRetail_Project1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail_Project1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class ProductController : Controller
    {
        private readonly AzureStorage _storage;

        public ProductController(AzureStorage storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storage.GetAllProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEntity product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    var imageUrl = await _storage.UploadImageAsync(ImageFile);
                    product.ImageUrl = imageUrl ?? string.Empty;
                }
                product.PartitionKey = "Product"; // Set PartitionKey
                product.RowKey = Guid.NewGuid().ToString(); // Generate unique RowKey

                await _storage.AddProductAsync(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Delete Product
        public async Task<IActionResult> Delete(string RowKey)
        {
            if (!string.IsNullOrEmpty(RowKey))
            {
                await _storage.DeleteProduct(RowKey, _storage.Get_productTable());
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Update(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();
            var product = await _storage.GetProductByIdAsync(rowKey);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string rowKey, ProductEntity product, IFormFile? imageFile)
        {
            if (rowKey != product.RowKey)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Upload new image if one was chosen
                if (imageFile != null)
                {
                    var imageUrl = await _storage.UploadImageAsync(imageFile);
                    product.ImageUrl = imageUrl ?? string.Empty;
                }

                product.PartitionKey = "Product"; // Ensure PartitionKey is set

                await _storage.UpdateProductAsync(product, imageFile!);

                return RedirectToAction(nameof(Index));
            }

            // if validation fails, redisplay the edit view with the current model
            return View(product);
        }
    }
}
