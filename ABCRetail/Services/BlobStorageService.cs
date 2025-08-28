using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace ABCRetail.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;
        public BlobStorageService(string storageConnectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _blobContainerClient.CreateIfNotExists();
        }

        // uplaod image create.cshtml

        public async Task<string> UploadPhotoAsync(string BlobName , Stream fileStream)
        {
            var blobClient = _blobContainerClient.GetBlobClient(BlobName);
            await blobClient.UploadAsync(fileStream, true);
            return GetBlobUriWithSas(BlobName);
        }

        // get blob uri with sas token 

        private string GetBlobUriWithSas(string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);

            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _blobContainerClient.Name,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMonths(1),
                };

                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("BlobClient cannot generate SAS URI. Ensure that the client is authorized with Shared Key credentials.");
            }
        }

        // delete photo

        public async Task DeletePhotoAsync(string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
