using System;
using System.IO;
using DashShared;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DashWebServer
{
    public static class BlobStorage
    {

        private static readonly CloudStorageAccount _account;
        private static readonly CloudBlobClient _client;
        private static readonly CloudBlobContainer _blobContainer;

        static BlobStorage()
        {
            try
            {
                // create the storage account and client
                _account = CloudStorageAccount.Parse(DashConstants.BlobConnectionString);
                _client = _account.CreateCloudBlobClient();

                // create the storage container if it doesn't exist
                _blobContainer = _client.GetContainerReference(DashConstants.BlobContainerName);
                _blobContainer.CreateIfNotExistsAsync().Wait();
                _blobContainer.SetPermissionsAsync(new BlobContainerPermissions
                {
                    // access documented here https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage.blob.blobcontainerpublicaccesstype?view=azurestorage-8.1.3
                    PublicAccess = BlobContainerPublicAccessType.Off 
                }).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void UploadBlobToContainer(string blobName)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(blobName);

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = File.OpenRead(@"path\myfile"))
            {
                blockBlob.UploadFromStreamAsync(fileStream);
            }
        }

        public static void DownloadBlobFromContainer(string blobName)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(blobName);

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStreamAsync(memoryStream);
            }
        }

        public static void DeleteBlob(string blobName)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(blobName);
            blockBlob.DeleteAsync();
        }
    }
}
