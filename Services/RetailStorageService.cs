using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StudentApplication.Services
{
    public class RetailStorageService
    {
        private readonly string _connectionString;
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly BlobContainerClient _blobContainer;
        private readonly QueueClient _queueClient;
        private readonly ShareClient _shareClient;

        public RetailStorageService(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString"];

            // Initialize clients
            _customerTable = new TableClient(_connectionString, "CustomerProfile");
            _productTable = new TableClient(_connectionString, "Product");
            _blobContainer = new BlobContainerClient(_connectionString, "product-images");
            _queueClient = new QueueClient(_connectionString, "order-processing");
            _shareClient = new ShareClient(_connectionString, "contracts");

            // Create if not exists
            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
            _blobContainer.CreateIfNotExists();
            _queueClient.CreateIfNotExists();
            _shareClient.CreateIfNotExists();
        }

        //Add Customer
        public async Task AddCustomerAsync(string id, string name, string email)
        {
            var entity = new TableEntity("Customer", id)
            {
                { "Name", name },
                { "Email", email }
            };
            await _customerTable.AddEntityAsync(entity);
        }

        //Add Product
        public async Task AddProductAsync(string id, string name, double price)
        {
            var entity = new TableEntity("Product", id)
            {
                { "Name", name },
                { "Price", price }
            };
            await _productTable.AddEntityAsync(entity);
        }

        //Upload Product Image
        public async Task UploadImageAsync(string fileName, Stream fileStream)
        {
            var blobClient = _blobContainer.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }

        //Download Product Image URL
        public string GetImageUrl(string fileName)
        {
            var blobClient = _blobContainer.GetBlobClient(fileName);
            return blobClient.Uri.ToString();
        }

        //Add Order Message to Queue
        public async Task AddOrderMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        //Upload Contract to Azure Files
        public async Task UploadContractAsync(string fileName, Stream fileStream)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadAsync(fileStream);
        }
    }
}
