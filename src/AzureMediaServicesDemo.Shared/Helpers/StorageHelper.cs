using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaServicesDemo.Shared
{
    public interface IStorageHelper
    {
        Task CopyBlobAsync(Uri sasUri, string fileToUpload);

        CloudTable GetTable();

        Task InsertTableRecord(string name, string uri);
    }

    /// <summary>
    /// Service to Handle Interactions with Azure Table Storage
    /// </summary>
    /// <remarks>
    /// This class allows the facilitation of adding and retrieving videos from the Video Repo. Video
    /// metadata is stored in Azure Table Storage. Information stored is Video Name and Streaming
    /// endpoint in Azure Media Services.
    /// </remarks>
    public class StorageHelper : IStorageHelper
    {
        private const string tableName = "videos";
        private const string containerName = "input";
        private readonly ConfigWrapper _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageHelper"/> class.
        /// </summary>
        /// <param name="config">Instance of ConfigWrapper which is Env Variables.</param>
        public StorageHelper(ConfigWrapper config)
        {
            _config = config;
        }

        /// <summary>
        /// Copies video from input container to Azure Media Services asset container
        /// </summary>
        /// <param name="sasUri">The uri of the destination container</param>
        /// <param name="fileToUpload">The name of the file to move to AMS asset container</param>
        /// <returns></returns>
        // <CopyBlobAsync>
        public async Task CopyBlobAsync(Uri sasUri, string fileToUpload)
        {
            CloudBlobContainer container = new CloudBlobContainer(sasUri);
            var destBlob = container.GetBlockBlobReference(fileToUpload);

            CloudBlobContainer sourceContainer = GetCloudBlobContainer(containerName);
            var sourceBlob = sourceContainer.GetBlockBlobReference(fileToUpload);

            var signature = sourceBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)

            });

            await destBlob.StartCopyAsync(new Uri(sourceBlob.Uri.AbsoluteUri + signature));
        }
        // </CopyBlobAsync>


        /// <summary>
        /// Gets reference to Azure Table Storage Table
        /// </summary>
        /// <returns>CloudTable object representing Azure Table Storage table</returns>
        // <GetTable>
        public CloudTable GetTable()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config.StorageConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            return tableClient.GetTableReference(tableName);
        }
        // </GetTable>

        /// <summary>
        /// Inserts Row into Azure Table Storage table
        /// </summary>
        /// <param name="name">The name of the video</param>
        /// <param name="uri">The uri representing the uri of the streaming endpoint</param>
        /// <returns></returns>
        // <InsertTableRecord>
        public async Task InsertTableRecord(string name, string uri)
        {
            var table = GetTable();

            await table.CreateIfNotExistsAsync();
            var video = new Video(Guid.NewGuid().ToString())
            {
                Name = name,
                Uri = uri
            };

            var insertOperation = TableOperation.Insert(video);
            await table.ExecuteAsync(insertOperation);
        }
        // </InsertTableRecord>

        /// <summary>
        /// Gets reference to Azure Blob Storage Container
        /// </summary>
        /// <param name="containerName">Name of container to get reference to</param>
        /// <returns>Reference to container</returns>
        // <GetCloudBlobContainer>
        private CloudBlobContainer GetCloudBlobContainer(string containerName)
        {
            CloudStorageAccount sourceStorageAccount = CloudStorageAccount.Parse(_config.StorageConnectionString);
            CloudBlobClient sourceCloudBlobClient = sourceStorageAccount.CreateCloudBlobClient();
            return sourceCloudBlobClient.GetContainerReference(containerName);
        }
        // </GetCloudBlobContainer>
    }
}
