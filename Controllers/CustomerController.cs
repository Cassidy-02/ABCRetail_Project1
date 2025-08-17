using ABCRetail_Project1.Models;
using ABCRetail_Project1.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail_Project1.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AzureStorage _storage;

        public CustomerController(AzureStorage storage)
        {
            _storage = storage;
        }

        public IActionResult Index()
        {
            var customers = _storage.GetCustomers();
            return View(customers);
        }

        public IActionResult Create()
        {
             return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEntity customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();

            await _storage.AddCustomerAsync(customer);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Delete Customer
        public async Task<IActionResult> Delete(string RowKey)
        {
            if (!string.IsNullOrEmpty(RowKey))
            {
                await _storage.DeleteCustomer(RowKey);
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
