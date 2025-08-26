using Azure;
using Azure.Data.Tables;
using System;  



namespace StudentApplication.Models
{
    public class StudentMark : ITableEntity
    {
        public string PartitionKey { get; set; } = "Student";

        public string RowKey { get; set; }

        public string Name { get; set; }

        public string Module { get; set; }

        public int Mark1 { get; set; }
        public int Mark2 { get; set; }
        public int Mark3 { get; set; }
        public int Mark4 { get; set; }

        public string ImageUrl { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
