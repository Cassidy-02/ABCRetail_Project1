using ABCRetail_Project1.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace ABCRetail_Project1.Services
{
    public class AzureStorage
    {
        private readonly string _connectionString;

        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        
        private readonly QueueClient _orderQueue;
        private readonly ShareClient _shareClient;
        private readonly BlobContainerClient _blobContainer;

        public AzureStorage(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString1"];

            //  Tables 
            var tableService = new TableServiceClient(_connectionString);
            _customerTable = tableService.GetTableClient("Customer");
            _productTable = tableService.GetTableClient("Product");
           

            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
           

            //  Queue 
            var queueName = config["AzureStorage:OrderQueue"] ?? "order";
            _orderQueue = new QueueClient(_connectionString, queueName);
            _orderQueue.CreateIfNotExists();

            //  File Share 
            var shareName = config["AzureStorage:FileShare"] ?? "sharedfiles";
            _shareClient = new ShareClient(_connectionString, shareName);
            _shareClient.CreateIfNotExists();

            //  Blob Container 
            var containerName = config["AzureStorage:BlobContainer"] ?? "image-media";
            _blobContainer = new BlobContainerClient(_connectionString, containerName);
            _blobContainer.CreateIfNotExists(PublicAccessType.Blob);
        }

        // Seed sample data for customers and products
        public async Task SeedDataAsync()
        {

            // --- Seed Customers ---
            var customers = new List<CustomerEntity>
            {
                new CustomerEntity { PartitionKey="Customer", RowKey= "1", FullName="John Doe", Email="john.doe@example.com", Phone="123-456-7890", Address="123 Elm Street", Country="US" },
                new CustomerEntity { PartitionKey="Customer", RowKey= "2", FullName="Jane Smith", Email="jane.smith@example.com", Phone="072-234-1235", Address="4 Oak Avenue", Country="South Africa" },
                new CustomerEntity { PartitionKey="Customer", RowKey= "3", FullName="Alex Jones", Email="alex.jones@example.com", Phone="555-123-4567", Address="789 Maple Road", Country= "Canada" },
                new CustomerEntity { PartitionKey="Customer", RowKey= "4", FullName="Lisa Wong", Email="lisa.wong@example.com", Phone="444-987-6543", Address="321 Pine Lane" , Country = "US"},
                new CustomerEntity { PartitionKey="Customer", RowKey= "5", FullName="Michael Brown", Email="michael.brown@example.com", Phone="222-333-4444", Address="654 Cedar Street", Country= "US" }
            };


            foreach (var c in customers)
            {
                await _customerTable.UpsertEntityAsync(c, TableUpdateMode.Replace);
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


            foreach (var p in products)
            {
                var localImagePath = Path.Combine("wwwroot/images", $"{p.RowKey}.png");
                if (File.Exists(localImagePath))
                {
                    var ext = Path.GetExtension(localImagePath).ToLower();
                    var blob = _blobContainer.GetBlobClient($"{p.RowKey}.png");
                    var headers = new BlobHttpHeaders
                    {
                        ContentType = (ext == ".png") ? "image/png" : "image/jpeg"
                    };
                    using var fileStream = File.OpenRead(localImagePath);
                    await blob.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers });
                    p.ImageUrl = blob.Uri.ToString();
                }
                await _productTable.UpsertEntityAsync(p, TableUpdateMode.Replace);
            }

            // Seed data into Queue

            var orders = new List<Order>
            {
                new Order { OrderId="Order-1001", CustomerName="John Doe", ProductId="PROD-1001",Quantity=3 },
                new Order { OrderId="Order-1002", CustomerName="Jane Smith", ProductId="PROD-1002",Quantity=5 },
                new Order { OrderId="Order-1003", CustomerName="Alex Jones", ProductId= "PROD-1003",Quantity=7 },
                new Order { OrderId="Order-1004", CustomerName="Lisa Wong", ProductId="PROD-1004",Quantity=10 },
                new Order { OrderId="Order-1005", CustomerName="Michael Brown", ProductId="PROD-1005",Quantity=14 }
            };

            foreach (var order in orders)
            {
                await EnqueueOrderAsync(order); // uses stock check method
            }

            
        }


        //Customers
        //Gets all customers from the table
        public async Task<List<CustomerEntity>> GetCustomersAsync()
        {
            var customers = new List<CustomerEntity>();
            await foreach (var entity in _customerTable.QueryAsync<CustomerEntity>())
            {
                customers.Add(entity);
            }
            return customers;
        }
        //Adds customer to the table
        public async Task AddCustomerAsync(CustomerEntity customer)
        {
            await _customerTable.UpsertEntityAsync(customer);
        }
        //Deletes customer by RowKey
        public async Task DeleteCustomer(string RowKey)
        {
            await _customerTable.DeleteEntityAsync("Customer", RowKey);
        }


        //Products
        public async Task<List<ProductEntity>> GetAllProductsAsync()
        {
           
            var products = new List<ProductEntity>();

            await foreach (var entity in _productTable.QueryAsync<ProductEntity>(p => p.PartitionKey == "Product"))
            {
                products.Add(entity);
            }

            return products;
        }

        //Adds product to the table
        public async Task AddProductAsync(ProductEntity product)
        {
            product.PartitionKey = "Product";

            if (string.IsNullOrWhiteSpace(product.RowKey))
            {
                product.RowKey = $"PROD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            }

            await _productTable.UpsertEntityAsync(product, TableUpdateMode.Replace);

            // Write log to Azure Files
            await WriteLogAsync("product",
                $"NEW PRODUCT ADDED: {product.RowKey} - {product.ProductName}, Price: R{product.Price}, Stock: {product.StockQuantity}");
        }

        public TableClient Get_productTable()
        {
            return _productTable;
        }

        //Deletes product by RowKey
        public async Task DeleteProduct(string RowKey, TableClient tableClient)
        {
            
            await _productTable.DeleteEntityAsync("Product", RowKey);
        }
        // Update product by RowKey
        public async Task UpdateProductAsync(ProductEntity product)
        {
            
            await _productTable.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        public async Task<ProductEntity?> GetProductByIdAsync(string rowKey)
        {
           
            try
            {
                var response = await _productTable.GetEntityAsync<ProductEntity>("Product", rowKey);
                return response.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }


        // Uploads image to Blob Storage and returns its URL
        public async Task<string> UploadImageAsync(IFormFile imageFile, string productId)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var ext = Path.GetExtension(imageFile.FileName).ToLower();
            var blob = _blobContainer.GetBlobClient($"{productId}{ext}");

            var headers = new BlobHttpHeaders
            {
                ContentType = (ext == ".png") ? "image/png" : "image/jpeg"
            };

            using var stream = imageFile.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

            return blob.Uri.ToString();
        }



        //Queue
        public async Task<List<Order>> PeekOrdersAsync()
        {
            var orders = new List<Order>();

            // Peek first 32 messages (Azure Queue limit per peek call)
            PeekedMessage[] messages = await _orderQueue.PeekMessagesAsync(32);

            foreach (var msg in messages)
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(msg.MessageText));
                    var order = JsonSerializer.Deserialize<Order>(json);
                    if (order != null)
                        orders.Add(order);
                }
                catch
                {
                    // ignore bad messages
                }
            }

            return orders
                .GroupBy(o => o.OrderId)
                .Select(g => g.First())
                .ToList(); ;
        }

        public async Task EnqueueOrderAsync(Order order)
        {
            var json = JsonSerializer.Serialize(order);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            await _orderQueue.SendMessageAsync(base64);

            await WriteLogAsync("orders", $"Order {order.OrderId} enqueued for Customer {order.CustomerName}");
        }

        
        public async Task<Order?> DequeueOrderAsync()
        {
            QueueMessage[] messages = await _orderQueue.ReceiveMessagesAsync(1);

            if (messages.Length == 0)
                return null;

            var message = messages[0];
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
            var order = JsonSerializer.Deserialize<Order>(json);

            if (order != null)
            {
                // Update product stock
                var product = await _productTable.GetEntityAsync<ProductEntity>("Product", order.ProductId);

                if (product != null)
                {
                    product.Value.StockQuantity -= order.Quantity;
                    if (product.Value.StockQuantity < 0)
                        product.Value.StockQuantity = 0; // prevent negative stock

                    await _productTable.UpdateEntityAsync(product.Value, product.Value.ETag, TableUpdateMode.Replace);
                }
            }

                // Remove from queue
                await _orderQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt);

            if(order != null)
               await WriteLogAsync("orders", $"Order {order.OrderId} dequeued for Customer {order.CustomerName}");

            return order;
        }
      


        // Log files
        public async Task<List<string>> ListFilesAsync()
        {
            var files = new List<string>();
            var root = _shareClient.GetRootDirectoryClient();

            await foreach (var item in root.GetFilesAndDirectoriesAsync()) 
            { 
                if (!item.IsDirectory) 
                    files.Add(item.Name); 
            }
           
            return files;
        }

      
        public async Task WriteLogAsync(string logType, string message)
        {
            try
            {
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var fileName = $"{logType}-{date}.log";

                 
                var rootDir = _shareClient.GetRootDirectoryClient();
                var fileClient = rootDir.GetFileClient(fileName);

                // Prepare log line
                var logLine = $"{DateTime.UtcNow:O} [{logType.ToUpper()}] {message}\n";
                var logBytes = Encoding.UTF8.GetBytes(logLine);

                long fileSize = 0;

                // Ensure file exists
                if (!await fileClient.ExistsAsync())
                {
                    await fileClient.CreateAsync(logBytes.Length);
                    await fileClient.UploadRangeAsync(new HttpRange(0, logBytes.Length), new MemoryStream(logBytes));
                }
            
                else
                {
                    var props = await fileClient.GetPropertiesAsync();
                    long currentLength = props.Value.ContentLength;
                    await fileClient.UploadRangeAsync(new HttpRange(currentLength, logBytes.Length), new MemoryStream(logBytes));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        public async Task<List<Order>> GetOrderStatusesAsync()
        {
            var orders = new List<Order>();

            // 1. Pending / Processing from queue
            var peekedOrders = await PeekOrdersAsync();
            foreach (var o in peekedOrders)
            {
                orders.Add(new Order
                {
                    OrderId = o.OrderId,
                    CustomerName = o.CustomerName,
                    ProductId = o.ProductId,
                    Quantity = o.Quantity,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow
                });
            }

            // 2. Completed / Deleted from logs
            var logFiles = await ListFilesAsync();
            foreach (var file in logFiles)
            {
                var fileClient = _shareClient.GetRootDirectoryClient().GetFileClient(file);

                if (!await fileClient.ExistsAsync())
                {
                    continue;
                }
                try
                {
                    var downloadResponse = await fileClient.DownloadAsync();
                    using var stream = new MemoryStream();

                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {

                            var parts = line.Split(']');
                            if (parts.Length < 2) continue;

                            var timestampPart = parts[0].TrimStart('[');

                            var message = parts[1].Trim();
                            var messageParts = message.Split(' ');

                            if (messageParts.Length < 2) continue;

                            var orderId = messageParts[1];

                            orders.Add(new Order
                            {
                                OrderId = orderId,
                                Status = message.Contains("dequeued") ? "Completed" : "Enqueued",
                                OrderDate = DateTime.TryParse(timestampPart,out var dt) ? dt : DateTime.UtcNow
                            });
                        }
                        catch
                        {
                            // ignore bad log lines
                        }
                    }
                }
                catch
                {
                    // ignore read errors
                    continue;
                }
            }

                // Remove duplicates: keep the latest status
                return orders
                .GroupBy(o => o.OrderId)
                .Select(g => g.OrderByDescending(x => x.OrderDate).First())
                .ToList();
        }


        public async Task<bool> ReduceInventoryAsync(string productId, int qty)
        {
            try {
                var productResponse = await _productTable.GetEntityAsync<ProductEntity>("Product", productId);

                if (productResponse == null || productResponse.Value.StockQuantity < qty)
                {
                    await WriteLogAsync("inventory", $"FAILED: Not enough stock for Product {productId}");
                    return false;
                }
                var product = productResponse.Value;

                product.StockQuantity -= qty;
                await _productTable.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

                await WriteLogAsync("inventory",
                    $"Product {productId} stock reduced by {qty}. New Qty: {product.StockQuantity}");

                return true;
            }
            catch (Exception ex)
            {
                // Log failure
                await WriteLogAsync("error", $"EXCEPTION in ReduceInventoryAsync for Product {productId}: {ex.Message}");
                return false;
            }
        }


      // Upload file to file share
        public async Task UploadFileAsync(IFormFile file)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(file.FileName);

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(stream);
        }

        // Download file from file share
        public async Task<Stream?> DownloadFileAsync(string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var download = await fileClient.DownloadAsync();
                return download.Value.Content;
            }
            return null;
        }


        public async Task DeleteFileAsync(string fileName)
        {
            var root = _shareClient.GetRootDirectoryClient();
            var fileClient = root.GetFileClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }

    }
}

