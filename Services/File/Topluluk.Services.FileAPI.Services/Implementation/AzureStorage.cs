using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Topluluk.Services.FileAPI.Services.Interface;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.FileAPI.Services.Implementation
{
	public class AzureStorage : Storage, IAzureStorage
    {
        private readonly BlobServiceClient _blobServiceClient;
        public AzureStorage(IConfiguration configuration)
		{
            string? connectionString = configuration["Storage:Azure"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Azure storage connection string can not be null.", nameof(configuration));
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        public async Task<List<string>> UploadAsync(string containerName, IFormFileCollection formFileCollection)
        {
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await _blobContainerClient.CreateIfNotExistsAsync();
            await _blobContainerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);

            List<string> datas = new();
            foreach (IFormFile file in formFileCollection)
            {
                string fileNewName = await FileRenameAsync(containerName, file.FileName, HasFile);
                BlobClient blobClient = _blobContainerClient.GetBlobClient(fileNewName);
                await blobClient.UploadAsync(file.OpenReadStream());
                datas.Add(blobClient.Uri.AbsoluteUri);
            }
            return datas;
        }
        public async Task<Response<string>> DeleteAsync(string containerName, string fileName)
        {
            try
            {
                BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                string fileNameWithExtension = fileName.Substring(fileName.LastIndexOf("/") + 1);
                BlobClient blobClient = _blobContainerClient.GetBlobClient(fileNameWithExtension);
                var a =  await blobClient.DeleteAsync();
                return await Task.FromResult(Response<string>.Success("Successfully deleted", ResponseStatus.Success));
            }
            catch (Exception e)
            {
                return await Task.FromResult(Response<string>.Fail($"Some error occurred: {e}", ResponseStatus.InitialError));
            }
        }

        public List<string> GetFiles(string containerName)
        {
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return _blobContainerClient.GetBlobs().Select(b => b.Name).ToList();
        }

        public new bool HasFile(string containerName, string fileName)
        {
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return _blobContainerClient.GetBlobs().Any(b => b.Name == fileName);
        }

        public async Task<Response<string>> UploadOneAsync(string containerName, IFormFile file)
        {
            BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await _blobContainerClient.CreateIfNotExistsAsync();
            await _blobContainerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
            string fileNewName = await FileRenameAsync(containerName, file.FileName, HasFile);
            BlobClient blobClient = _blobContainerClient.GetBlobClient(fileNewName);
            await blobClient.UploadAsync(file.OpenReadStream());
            return await Task.FromResult(Response<string>.Success(blobClient.Uri.AbsoluteUri,ResponseStatus.Success));
        }
    }
}

