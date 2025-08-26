using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration; //ALLOWS USE TO READ CONN STRING
using StudentApplication.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace StudentApplication.Services
{
    public class StudentStorageService
    {
        //this allow us to interact with azure table storage
        private readonly TableClient _tableClient;
        //this allow us to interact with azure blob storage
        private readonly BlobContainerClient _blobContainerClient;
        public StudentStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];

            // Initialize the TableClient CRUD operations
            _tableClient = new TableServiceClient(connectionString)
                .GetTableClient("StudentMarks");

            _tableClient.CreateIfNotExists();

            // Initialize the BlobContainerClient for file uploads
            _blobContainerClient = new BlobServiceClient(connectionString)
                .GetBlobContainerClient("studentimages");

            _blobContainerClient.CreateIfNotExists();

        }

        // It takes the student data, image stream, and filename as input
        public async Task AddStudentAsync(StudentMark student, Stream imageStream, string fileName)
        {
            // We prepare to upload the image file to Azure Blob Storage by creating a blob client
            var blobClient = _blobContainerClient.GetBlobClient(fileName);

            // We upload the image stream to Azure with overwrite set to true
            // If a file with the same name already exists, it will be replaced
            await blobClient.UploadAsync(imageStream, overwrite: true);

            // After uploading, we store the public URL of the image into the student object
            // This URL will be used to display the image later in the web app
            student.ImageUrl = blobClient.Uri.ToString();

            // We generate a unique RowKey using a GUID to ensure each student record is uniquely identified
            student.RowKey = Guid.NewGuid().ToString();

            // We save the student object to Azure Table Storage
            // Azure will store it as a new row in the "StudentMarks" table
            await _tableClient.AddEntityAsync(student);
        }
        // It returns a list of StudentMark objects
        public async Task<List<StudentMark>> GetStudentsAsync()
        {
            // We create an empty list to hold the student records we retrieve
            var students = new List<StudentMark>();

            // This loops through all rows in the "StudentMarks" table asynchronously
            
            await foreach (var entity in _tableClient.QueryAsync<StudentMark>())
            {
                // Add each student record found in the table to the list
                students.Add(entity);
            }

            // Once all records are added, return the list back to the caller (e.g., controller)
            return students;
        }
    }

}

