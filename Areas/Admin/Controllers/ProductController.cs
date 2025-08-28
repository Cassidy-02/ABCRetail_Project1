using Microsoft.AspNetCore.Mvc;
using ABCRetail_Project1.Services;
using ABCRetail_Project1.Models;

namespace ABCRetail_Project1.Areas.Admin.Controllers
{
    [Area("Admin")]
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
        public async Task<IActionResult> Create(ProductEntity product, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

             if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Pass the IFormFile and product.RowKey to UploadImageAsync 
                    var imageUrl = await _storage.UploadImageAsync(ImageFile, product.RowKey);
                    product.ImageUrl = imageUrl;
                }
            else
                {
                    ModelState.AddModelError("ImageFile", "Please upload an image.");
                    return View(product);
                }
            try
            {
                // Call AzureStorage to add the product
                await _storage.AddProductAsync(product);

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the product: " + ex.Message);
                return View(product);
            }    
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
        public async Task<IActionResult> Update(string RowKey)
        {
            if (RowKey == null) return NotFound();

            var product = await _storage.GetProductByIdAsync(RowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string RowKey, ProductEntity product, IFormFile imageFile)
        {
            if (RowKey != product.RowKey)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Upload new image if one was chosen
                if (imageFile != null)
                {
                    product.ImageUrl = await _storage.UploadImageAsync(imageFile, product.RowKey);
                }

                await _storage.UpdateProductAsync(product);
                return RedirectToAction(nameof(Index));
            }

            // if validation fails, redisplay the edit view with the current model
            return View(product);
        }
    }
}
