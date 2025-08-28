using ABCRetail_Project1.Models;
using ABCRetail_Project1.Services;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace ABCRetail_Project1.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly AzureStorage _storage;

        public OrderController(AzureStorage storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storage.GetOrderStatusesAsync();
            return View(orders);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Add new order
        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            if(ModelState.IsValid)
                if (ModelState.IsValid)
                {
                    order.OrderId = Guid.NewGuid().ToString();
                    order.OrderDate = DateTime.UtcNow;

                    await _storage.EnqueueOrderAsync(order);
                    return RedirectToAction(nameof(Index));
                }
            return View(order);

            
        }

        // Process order
        public async Task<IActionResult> Process()
        {
            var order = await _storage.DequeueOrderAsync();
            if (order == null)
            {
                TempData["Message"] = "No orders in the queue.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Message"] = $"Processed order {order.OrderId} for {order.Quantity}x Product {order.ProductId}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
