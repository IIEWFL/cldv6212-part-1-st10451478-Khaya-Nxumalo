using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Order : ITableEntity
    {
        public string? PartitionKey { get; set; } = "Order";
        public string? RowKey { get; set; }       
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? ProductId { get; set; }
        public string? CustomerId { get; set; }
        public string? Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}