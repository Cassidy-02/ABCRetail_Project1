using ABCRetail_Project1.Models;
using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ABCRetail_Project1.Services
{
    public class AzureStorage(IConfiguration config)
    {
        private readonly string _connectionString = config["AzureStorage:ConnectionString1"];
        private readonly string _customerTable = config["AzureStorage:CustomerTable"];
        private readonly string _productTable = config["AzureStorage:ProductTable"];
        private readonly string _blobContainer = config["AzureStorage:BlobContainer"];

        // Seed sample data for customers and products
        public async Task SeedDataAsync()
        {
            var customerClient = new TableClient(_connectionString, _customerTable);
            var productClient = new TableClient(_connectionString, _productTable);
            await customerClient.CreateIfNotExistsAsync();
            await productClient.CreateIfNotExistsAsync();

            var blobContainer = new BlobContainerClient(_connectionString, _blobContainer);
            await blobContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // --- Seed Customers ---
            var customers = new List<CustomerEntity>
            {
                new CustomerEntity { PartitionKey="Customer", RowKey="1", FullName="John Doe", Email="john.doe@example.com", Phone="123-456-7890", Address="123 Elm Street" },
                new CustomerEntity { PartitionKey="Customer", RowKey="2", FullName="Jane Smith", Email="jane.smith@example.com", Phone="987-654-3210", Address="456 Oak Avenue" },
                new CustomerEntity { PartitionKey="Customer", RowKey="3", FullName="Alex Jones", Email="alex.jones@example.com", Phone="555-123-4567", Address="789 Maple Road" },
                new CustomerEntity { PartitionKey="Customer", RowKey="4", FullName="Lisa Wong", Email="lisa.wong@example.com", Phone="444-987-6543", Address="321 Pine Lane" },
                new CustomerEntity { PartitionKey="Customer", RowKey="5", FullName="Michael Brown", Email="michael.brown@example.com", Phone="222-333-4444", Address="654 Cedar Street" }
            };

            
            foreach (var c in customers)
            {
                await customerClient.UpsertEntityAsync(c);
            }
           


            // --- Seed Products ---
            var products = new List<ProductEntity>
            {
                new ProductEntity { PartitionKey = "Product", RowKey="PROD-1001", ProductName="Wireless Bluetooth Headphones", Description="Noise-cancelling headphones, 20h battery life", Price=250, StockQuantity=50 },
                new ProductEntity { PartitionKey = "Product", RowKey="PROD-1002", ProductName="Smart Fitness Watch", Description="Waterproof, heart rate monitor, GPS", Price=300, StockQuantity=30 },
                new ProductEntity { PartitionKey = "Product", RowKey="PROD-1003", ProductName="Portable Bluetooth Speaker", Description="Compact, 10h battery, waterproof", Price=200, StockQuantity=75 },
                new ProductEntity { PartitionKey = "Product", RowKey="PROD-1004", ProductName="Wireless Charging Pad", Description="Fast charging, compatible with all Qi devices", Price=139.99, StockQuantity=100 },
                new ProductEntity { PartitionKey = "Product", RowKey="PROD-1005", ProductName="Noise Cancelling Earbuds", Description="In-ear, 15h battery life, IPX7 waterproof", Price=89.99, StockQuantity=60 }
            };

            string seedFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            foreach (var p in products)
            {
                var localImagePath = Path.Combine("wwwroot/images", $"{p.RowKey}.png");
                if (File.Exists(localImagePath))
                {
                    var ext = Path.GetExtension(localImagePath).ToLower();
                    var blob = blobContainer.GetBlobClient($"{p.RowKey}.png");
                    var headers = new BlobHttpHeaders
                    {
                        ContentType = (ext == ".png") ? "image/png" : "image/jpeg"
                    };
                    using var fileStream = File.OpenRead(localImagePath);
                    await blob.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers, TransferOptions = new StorageTransferOptions { }, });
                    p.ImageUrl = blob.Uri.ToString();
                }
                await productClient.UpsertEntityAsync(p);
            }
        }

        public List<CustomerEntity> GetCustomers()
        {
            var client = new TableClient(_connectionString, _customerTable);
            return client.Query<CustomerEntity>().ToList();
        }
        public List<ProductEntity> GetProducts()
        {
            var client = new TableClient(_connectionString, _productTable);
            return client.Query<ProductEntity>().ToList();
        }

    }
}

