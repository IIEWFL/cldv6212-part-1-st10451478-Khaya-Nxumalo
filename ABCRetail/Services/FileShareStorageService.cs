using Azure;
using Azure.Storage.Files.Shares;

namespace ABCRetail.Services
{
    public class FileShareStorageService
    {
        private readonly ShareClient _shareClient;

        public FileShareStorageService(string storageConnectionString, string shareName)
        {
            _shareClient = new ShareClient(storageConnectionString, shareName);
            _shareClient.CreateIfNotExists();
        }
        // Upload file to file share

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);


            //Handling the CSV
            long position = 0;
            int bufferSize = 4 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                using var memoryStream = new MemoryStream(buffer, 0, bytesRead);
                await fileClient.UploadRangeAsync(
                    Azure.Storage.Files.Shares.Models.ShareFileRangeWriteType.Update,
                    new HttpRange(position, bytesRead),
                    memoryStream
                    );
                position += bytesRead;
            }
        }
    }
}
