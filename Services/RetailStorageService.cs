using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Azure.Storage;

namespace StudentApplication.Services
{
    public class RetailStorageService
    {
        private readonly string _connectionString;

        // Tables
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;

        // Blobs
        private readonly BlobContainerClient _blobContainer;

        // Queues
        private readonly QueueClient _queueClient;

        // Files
        private readonly ShareClient _shareClient;

        public RetailStorageService(IConfiguration config)
        {
            _connectionString = config["AzureStorage:ConnectionString"];

            // Tables (with exponential retry) 
            var tableOptions = new TableClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 5,
                    Delay = TimeSpan.FromMilliseconds(300),
                    MaxDelay = TimeSpan.FromSeconds(5)
                }
            };

            _customerTable = new TableClient(_connectionString, "CustomerProfile", tableOptions);
            _productTable = new TableClient(_connectionString, "Product", tableOptions);
            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();

            // Blobs 
            var blobOptions = new BlobClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 5,
                    Delay = TimeSpan.FromMilliseconds(300),
                    MaxDelay = TimeSpan.FromSeconds(5)
                }
            };

            _blobContainer = new BlobContainerClient(_connectionString, "product-images", blobOptions);
            _blobContainer.CreateIfNotExists(PublicAccessType.Blob);

            // Queues (base64 + retry) 
            var queueOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 5,
                    Delay = TimeSpan.FromMilliseconds(300),
                    MaxDelay = TimeSpan.FromSeconds(5)
                }
            };

            _queueClient = new QueueClient(_connectionString, "order-processing", queueOptions);
            _queueClient.CreateIfNotExists();

            // Files 
            _shareClient = new ShareClient(_connectionString, "contracts");
            _shareClient.CreateIfNotExists();
        }

        // =========================================================
        // 1) TABLES — scalable partitioning + idempotent upsert
        // =========================================================

        // Add/Update Customer (Upsert). Partition by first letter of name to spread load.
        public async Task AddCustomerAsync(string id, string name, string email)
        {
            string partitionKey = string.IsNullOrWhiteSpace(name) ? "Z_MISC" : name[..1].ToUpperInvariant();
            string rowKey = id;

            var entity = new TableEntity(partitionKey, rowKey)
            {
                { "Name", name },
                { "Email", email },
                { "UpdatedUtc", DateTime.UtcNow }
            };

            await _customerTable.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }

        // Add/Update Product (Upsert). Partition by first letter of product name.
        public async Task AddProductAsync(string id, string name, double price)
        {
            string partitionKey = string.IsNullOrWhiteSpace(name) ? "Z_MISC" : name[..1].ToUpperInvariant();
            string rowKey = id;

            var entity = new TableEntity(partitionKey, rowKey)
            {
                { "Name", name },
                { "Price", price },
                { "UpdatedUtc", DateTime.UtcNow }
            };

            await _productTable.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }

        // =========================================================
        // 2) BLOBS — smarter naming; returns URL via GetImageUrl
        // =========================================================

        // Upload Product Image (safe, unique blob name). Overwrite enabled for deterministic rebuilds.
        public async Task UploadImageAsync(string fileName, Stream fileStream)
        {
            // Create a unique, safe blob name to avoid collisions and allow easy cleanup
            string safeFile = Path.GetFileName(fileName);
            string blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFile}";

            var blobClient = _blobContainer.GetBlobClient(blobName);

            // Try to infer content type from extension (optional lightweight)
            string? contentType = TryGuessContentType(safeFile);
            var headers = new BlobHttpHeaders();
            if (!string.IsNullOrWhiteSpace(contentType))
                headers.ContentType = contentType;

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = headers,
                TransferOptions = new StorageTransferOptions
                {
                    MaximumConcurrency = 4
                }
            });

        }

        public string GetImageUrl(string blobName)
        {
            var blobClient = _blobContainer.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }

        // Overload that returns (blobName, url) — use this in new code paths.
        public async Task<(string BlobName, Uri Url)> UploadImageWithResultAsync(string fileName, Stream fileStream, string? contentType = null)
        {
            string safeFile = Path.GetFileName(fileName);
            string blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFile}";

            var blobClient = _blobContainer.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders();
            headers.ContentType = !string.IsNullOrWhiteSpace(contentType)
                ? contentType
                : TryGuessContentType(safeFile) ?? "application/octet-stream";

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = headers,
                TransferOptions = new StorageTransferOptions { MaximumConcurrency = 4 }
            });

            return (blobName, blobClient.Uri);
        }

        // =========================================================
        // 3) QUEUES — write (enqueue) and read (dequeue one)
        // =========================================================

        public async Task AddOrderMessageAsync(string message)
        {
            // If "message" is already JSON, we send it as-is. Otherwise, wrap it for consistency.
            string payload = IsJson(message)
                ? message
                : JsonSerializer.Serialize(new { text = message, whenUtc = DateTime.UtcNow });

            await _queueClient.SendMessageAsync(payload);
        }

        // Helper to read ONE message and delete it (at-least-once semantics).
        // Useful for manual testing or a simple background processor.
        public async Task<string?> DequeueOneOrderMessageAsync()
        {
            QueueMessage[] msgs = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: TimeSpan.FromSeconds(30));
            if (msgs is { Length: > 0 })
            {
                var m = msgs[0];
                await _queueClient.DeleteMessageAsync(m.MessageId, m.PopReceipt);
                return m.MessageText;
            }
            return null;
        }

        // =========================================================
        // 4) FILES — upload contract (root or per-customer folder)
        // =========================================================

        // Upload Contract to root of share 
        public async Task UploadContractAsync(string fileName, Stream fileStream)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();

            // Ensure the directory exists (root should exist but this is harmless)
            await directoryClient.CreateIfNotExistsAsync();

            string safe = Path.GetFileName(fileName);
            var fileClient = directoryClient.GetFileClient(safe);

            long length = fileStream.Length;
            await fileClient.CreateAsync(length);
            await fileClient.UploadAsync(fileStream);
        }

        // Optional overload: organize contracts per customer to scale directories cleanly.
        public async Task UploadContractAsync(string customerId, string fileName, Stream fileStream, bool organizeByCustomer)
        {
            if (!organizeByCustomer)
            {
                await UploadContractAsync(fileName, fileStream);
                return;
            }

            var root = _shareClient.GetRootDirectoryClient();
            var dir = root.GetSubdirectoryClient(customerId);
            await dir.CreateIfNotExistsAsync();

            string safe = Path.GetFileName(fileName);
            var fileClient = dir.GetFileClient(safe);

            long length = fileStream.Length;
            await fileClient.CreateAsync(length);
            await fileClient.UploadAsync(fileStream);
        }

        // =========================================================
        // Helpers
        // =========================================================
        private static string? TryGuessContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".json" => "application/json",
                _ => null
            };
        }

        private static bool IsJson(string s)
        {
            s = s?.Trim() ?? string.Empty;
            if (s.Length < 2) return false;
            return (s.StartsWith("{") && s.EndsWith("}")) || (s.StartsWith("[") && s.EndsWith("]"));
        }
    }
}
