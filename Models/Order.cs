namespace ABCRetail_Project1.Models
{
    public class Order
    {
       
            public string OrderId { get; set; } = Guid.NewGuid().ToString();
            public string ProductId { get; set; }
            public string CustomerName { get; set; }
            public string ProductName { get; set; }
            public string Status { get; set; }

            public DateTime OrderDate { get; set; } = DateTime.UtcNow;
            public int Quantity { get; set; }
        
    

    }
}
