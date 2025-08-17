using Azure;
using Azure.Data.Tables;

namespace ABCRetail_Project1.Models
{
    public class CustomerEntity : ITableEntity
    {
        //PartitionKey = "Customers"
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; } 
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Country { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
