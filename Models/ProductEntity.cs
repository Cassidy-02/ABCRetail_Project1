using Azure;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;
using ITableEntity = Azure.Data.Tables.ITableEntity;

namespace ABCRetail_Project1.Models
{
    public class ProductEntity : ITableEntity
    {
        public required string PartitionKey { get; set; }
        public required string RowKey { get; set; }
        public required string ProductName { get; set; }
        public required string Description { get; set; }
       
        public double Price { get;set; }
        public  string ImageUrl { get; set; }
        public int StockQuantity { get; set; }


        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

      
    }
}
