using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSP_assignment.Models
{
    internal class ImageJobEntity: ITableEntity
    {
        public string PartitionKey { get; set; } = "ImageJob";
        public string RowKey { get; set; } // RowKey will be the ImageId
        public string JobId { get; set; }
        public string Status { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
