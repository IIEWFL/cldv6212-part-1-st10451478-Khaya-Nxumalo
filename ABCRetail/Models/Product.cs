using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Product : ITableEntity
    {
        public string? PartitionKey { get; set; } 
        public string? RowKey { get; set; }      
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public int InventoryCount { get; set; }
    }
}