
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMediaServicesDemo.Shared
{
    public class Video : TableEntity
    {
        public Video(string id)
        {
            this.PartitionKey = "Video";
            this.RowKey = id;
        }

        public Video() { }

        public string Name { get; set; }

        public string Uri { get; set; }
    }
}
