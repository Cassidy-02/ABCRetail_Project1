using ABCRetail_Project1.Models;
using ABCRetail_Project1.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail_Project1.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShopController : Controller
    {
        private readonly AzureStorage _storage;

        public ShopController(AzureStorage storage)
        {
            _storage = storage;
        }

        // Show all products
        public async Task<IActionResult> Index()
        {
            var product = await _storage.GetAllProductsAsync();
            return View(product);
        }

        //View product Details
        public async Task<IActionResult> Details(string id)
        {
            var product = await _storage.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
           
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> PlaceOrder(string id)
        {
            var product = await _storage.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        //Place Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string id, int quantity)
        {
            var product = await _storage.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check stock before processing
            var reduced = await _storage.ReduceInventoryAsync(product.RowKey, quantity);
            if (!reduced)
            {
                TempData["ErrorMessage"] = "Not enough stock available.";
                return RedirectToAction("Details", new { id });
            }

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ProductId = product.RowKey,
                ProductName = product.ProductName,
                Quantity = quantity,
                CustomerName = "John Doe", 
                OrderDate = DateTime.UtcNow
            };

            // Send to Azure Queue
            await _storage.EnqueueOrderAsync(order);

            // Log order event
            await _storage.WriteLogAsync("orders", $"order {order.OrderId} placed for product {product.RowKey},Qty{quantity}");

            // Reduce stock and update product table
            product.StockQuantity -= quantity;
            await _storage.UpdateProductAsync(product, null);

            // Redirect to confirmation page
            return RedirectToAction("Confirmation",new {orderId = order.OrderId});
        }

        [HttpGet]
        public IActionResult Confirmation(string orderId)
        {
            
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}
