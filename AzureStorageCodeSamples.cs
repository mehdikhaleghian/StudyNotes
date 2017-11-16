using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace GettingStartedWithBlobs
{
    class Person : TableEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
    class Program
    {
        private static readonly string ContainerName = "mycontainer";
        private static readonly string ConnectionStringName = "StorageConnectionString";
        private static readonly string BlobName = "myblob";
        static void Main(string[] args)
        {
            //CreateBlockBlob();
            //ListBlobItems();
            //ListTables();
            //    WorkWithQueue();
            var storageAccount = GetStorageAccount("mkhstorageaccount",
                "M+Idxczo1nqtw/GNWujlhvLn+qjBqOckNcqIsOd2TghAN4jC6CAfRxx3Sw5yekwkQMeT6UCKm3GDM7QAkwec7A==");
            //BasicBlockOperation(storageAccount);
            //BasiCTableOperation(storageAccount);
            //WorkWithQueue();
            InsertIntoPeopleTable(storageAccount);
            Console.ReadKey();
        }

        private static void InsertIntoPeopleTable(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var peopleTable = tableClient.GetTableReference("people");
            peopleTable.CreateIfNotExists();
            TableOperation insertPerson = TableOperation.Insert(new Person { Name = "Mehdi", Age = 35, PartitionKey = "Mehdi", RowKey = "Garahani" });
            var result = peopleTable.Execute(insertPerson);

        }

        private static void BasiCTableOperation(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var metricsTable = tableClient.GetTableReference("$MetricsHourPrimaryTransactionsBlob");
            var query = metricsTable.CreateQuery<DynamicTableEntity>().ToList();
            foreach (var entity in query)
            {
                foreach (var property in entity.Properties)
                {

                }
            }

        }

        private static CloudStorageAccount GetStorageAccount(string name, string key)
        {
            StorageCredentials creds = new StorageCredentials(name, key);
            var storageAccount = new CloudStorageAccount(creds, true);
            Console.WriteLine($"blob endpoint:{storageAccount.BlobEndpoint}");
            Console.WriteLine($"queue endpoint:{storageAccount.QueueEndpoint}");
            Console.WriteLine($"table endpoint:{storageAccount.TableEndpoint}");
            return storageAccount;
        }


        private static void BasicBlockOperation(CloudStorageAccount storageAccount)
        {
            var blockClient2 = new CloudBlobClient(new Uri("232"),new StorageCredentials(""));

            var blobClient = storageAccount.CreateCloudBlobClient();
            Console.WriteLine("Containers:");

            var containers = blobClient.ListContainers();
            foreach (var container in containers)
            {
                Console.WriteLine($"            {container.Name}");
            }

            var testContainer = blobClient.GetContainerReference("testcontainer");
            testContainer.CreateIfNotExists();

            var blobToUpload = testContainer.GetBlockBlobReference("DatabasePermissions.png");
            blobToUpload.UploadFromFile(@"c:\mehdi.jpg");

            var blobs = testContainer.ListBlobs();
            foreach (var blob in blobs)
            {
                Console.WriteLine($"absolute url is {blob.Uri.AbsoluteUri}");
            }
        }


        private static void WorkWithQueue()
        {
            var uri = new Uri("https://mkhstorageaccount.queue.core.windows.net");
            StorageCredentials storageCredentials = new StorageCredentials("mkhstorageaccount",
                "M+Idxczo1nqtw/GNWujlhvLn+qjBqOckNcqIsOd2TghAN4jC6CAfRxx3Sw5yekwkQMeT6UCKm3GDM7QAkwec7A==");
            var queueClient = new CloudQueueClient(uri, storageCredentials);
            var queue = queueClient.GetQueueReference("orders");
            var created = queue.CreateIfNotExists();
        }

        private static void ListTables()
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(ConnectionStringName));
                var tableClient = storageAccount.CreateCloudTableClient();
                tableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry();
                var tables = tableClient.ListTables();

                foreach (var table in tables)
                {
                    TablePermissions tablePermission = new TablePermissions();
                    tablePermission.SharedAccessPolicies.Add(new KeyValuePair<string, SharedAccessTablePolicy>("01", value));
                    table.SetPermissions(tablePermission);
                    Console.WriteLine($"table with name={table.Name}");
                }
            }
            catch (StorageException ex)
            {
                throw;
            }
        }

        private static void ListBlobItems()
        {

            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(ConnectionStringName));
            SharedAccessAccountPolicy storagePolicy = new SharedAccessAccountPolicy
            {
                Permissions = SharedAccessAccountPermissions.Add | SharedAccessAccountPermissions.Delete,
                Protocols = SharedAccessProtocol.HttpsOnly,
                Services = SharedAccessAccountServices.Blob | SharedAccessAccountServices.Queue,
                SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1)
            };
            var storageSas = storageAccount.GetSharedAccessSignature(storagePolicy);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ContainerName);
            
            var blobs = container.ListBlobs(null, false);
            TraverseBlobs(blobs);

            void TraverseBlobs(IEnumerable<IListBlobItem> blobItems)
            {
                foreach (var blob in blobItems)
                {
                    switch (blob)
                    {
                        case CloudBlockBlob block:
                            Console.WriteLine($"block blob with Length:{block.Properties.Length} Uri:{block.Uri}");
                            using (var fileStream = File.OpenWrite(@"C:\Aria\" + Guid.NewGuid()))
                            {
                                block.DownloadToStream(fileStream);
                            }
                            break;
                        case CloudBlobDirectory directory:
                            Console.WriteLine($"blob directory with Uri:{directory.Uri}");
                            TraverseBlobs(directory.ListBlobs());
                            break;
                        case CloudPageBlob page:
                            Console.WriteLine($"page blob with Length:{page.Properties.Length} Uri:{page.Uri}");
                            break;
                    }
                }
            }
        }

        private static void CreateBlockBlob()
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(ConnectionStringName));

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            //blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 4;
            //blobClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes

            //retrieve a reference to a container
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);

            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(@"images2\" + BlobName);
            
            using (var fileStream = File.OpenRead(@"C:\Aria\test.txt"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
        }
    }
}
