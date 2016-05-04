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

            program.ListBlobs(container, true);

            var blobNames = program.GetBlobNames(container);

            foreach (var blob in blobNames)
            {
                program.DownloadBlobs(container, blob);
            }

            var toDelete = blobNames.Where(b => b.Contains("deleteMe"));

            foreach (var blob in toDelete)
            {
                program.DeleteBlob(container, blob);
            }

            blobNames = program.GetBlobNames(container);

            var accessibleUris = program.GetAccessibleUris(container);
        }

        private MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        public IEnumerable<string> GetAccessibleUris(CloudBlobContainer container)
        {
            var sasToken = GetReadSASToken(container);
            var uris = GetBlobUris(container);
            var accessibleUris = uris.Select(uri => uri + sasToken).ToList();
            return accessibleUris;
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

        public IEnumerable<string> GetBlobNames(CloudBlobContainer container)
        {
            var blobRefs = new List<string>();
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    blobRefs.Add(blob.Name);
                }
            }
            return blobRefs;
        }

        public IEnumerable<string> GetBlobUris(CloudBlobContainer container)
        {
            var uris = new List<string>();
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    uris.Add(blob.Uri.AbsoluteUri);
                }
            }
            return uris;
        }

        public void ListBlobs(CloudBlobContainer container, bool useFlatListing = false)
        {
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, useFlatListing))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
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

        public string GetReadSASToken(CloudBlobContainer container)
        {
            // Get the current permissions for the blob container.
            BlobContainerPermissions blobPermissions = container.GetPermissions();

            // Clear the container's shared access policies to avoid naming conflicts.
            blobPermissions.SharedAccessPolicies.Clear();

            // The new shared access policy provides read access to the container for 24 hours.
            blobPermissions.SharedAccessPolicies.Add("mypolicy", new SharedAccessBlobPolicy()
            {
                // To ensure SAS is valid immediately, don’t set the start time.
                // This way, you can avoid failures caused by small clock differences.
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read
            });

            // The public access setting explicitly specifies that
            // the container is private, so that it can't be accessed anonymously.
            blobPermissions.PublicAccess = BlobContainerPublicAccessType.Off;

            // Set the new stored access policy on the container.
            container.SetPermissions(blobPermissions);

            // Get the shared access signature token to share with users.
            string sasToken =
               container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "mypolicy");
            return sasToken;
        }
    }
}
