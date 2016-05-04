using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

// Namespace for Blob storage types

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = File.ReadAllText("C:\\ConnectionStrings\\StorageAccountConnectionString.txt");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("mycontainer");

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            var program = new Program();

            program.UploadToBlobs(container);

            var blobNames = program.ListBlobs(container, true);

            foreach (var blob in blobNames)
            {
                program.DownloadBlobs(container, blob);
            }

            var toDelete = blobNames.Where(b => b.Contains("deleteMe"));

            foreach (var blob in toDelete)
            {
                program.DeleteBlob(container, blob);
            }

            blobNames = program.ListBlobs(container, true);
        }

        private MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        public void UploadToBlobs(CloudBlobContainer container)
        {
            for (var i = 0; i < 10; i++)
            {
                // Retrieve reference to a blob named "myblob".

                CloudBlockBlob blockBlob = container.GetBlockBlobReference($"fakeDomain{i}.com");

                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var stream = GenerateStreamFromString($"this is some test blob content {i}"))
                {
                    blockBlob.UploadFromStream(stream);
                }

                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference($"deleteMe{i}");

                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var stream = GenerateStreamFromString($"this is some test blob content to delete {i}"))
                {
                    blockBlob2.UploadFromStream(stream);
                }
            }
        }

        public IEnumerable<string> ListBlobs(CloudBlobContainer container, bool useFlatListing = false)
        {
            var blobRefs = new List<string>();
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, useFlatListing))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
                    blobRefs.Add(blob.Name);
                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;

                    Console.WriteLine("Page blob of length {0}: {1}", pageBlob.Properties.Length, pageBlob.Uri);

                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;

                    Console.WriteLine("Directory: {0}", directory.Uri);
                }
            }
            return blobRefs;
        }

        public void DownloadBlobs(CloudBlobContainer container, string blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Save blob contents to a file.
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                var content = Encoding.UTF8.GetString(memoryStream.ToArray());
                Console.WriteLine("{0}: {1}", blobName, content);
            }
        }

        public void DeleteBlob(CloudBlobContainer container, string blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            blockBlob.Delete();
        }
    }
}
