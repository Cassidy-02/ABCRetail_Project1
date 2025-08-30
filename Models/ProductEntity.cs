using Azure;
using Azure.Data.Tables;



namespace ABCRetail_Project1.Models
{
    public class ProductEntity : ITableEntity
    {
        public required string PartitionKey { get; set; } 
        public required string RowKey { get; set; } 
        public required string ProductName { get; set; }
        public required string Description { get; set; }
       
        public double Price { get;set; }
        public  string ImageUrl { get; set; }  // store blob url 
        public int StockQuantity { get; set; }


        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

      
    }
}
