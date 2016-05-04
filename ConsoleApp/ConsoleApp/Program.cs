using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = File.ReadAllText("C:\\ConnectionStrings\\StorageAccountConnectionString.txt");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString); 
        }
    }
}
