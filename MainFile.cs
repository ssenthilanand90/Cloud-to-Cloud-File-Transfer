using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using System.IO;
using System.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;

namespace ConsoleApplication2
{
    class Program
    {
        private static string amazonBucketName = ConfigurationSettings.AppSettings["AmazonBucketName"];
        private static readonly string amazonObjectUrl = ConfigurationSettings.AppSettings["AmazonSourceObjectURL"];
        
        // Azure Details 
        private static readonly string azureStorageAccountName = ConfigurationSettings.AppSettings["DestinationStorageAccountName"];
        private static readonly string azureStorageAccountKey = ConfigurationSettings.AppSettings["DestinationStorageAccountKey"];
        private static readonly string azureBlobContainerName = ConfigurationSettings.AppSettings["DestinationBlobContainerName"];
        private static readonly string azureBlobName = ConfigurationSettings.AppSettings["DestinationBlobName"];
        static void Main(string[] args)
        {

            CloudStorageAccount storageAccount =

                      new CloudStorageAccount(new StorageCredentials(azureStorageAccountName, azureStorageAccountKey), true);

            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = cloudBlobClient.GetContainerReference(azureBlobContainerName);
            Console.WriteLine("Trying to create the blob container....");
            blobContainer.CreateIfNotExists();
            Console.WriteLine("Blob container created successfully");
            Console.WriteLine("------------------------------------");
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(azureBlobName);
            Console.WriteLine("Created a reference for block blob in Windows Azure....");
            Console.WriteLine("Blob Uri: " + blockBlob.Uri.AbsoluteUri);
            Console.WriteLine("Now trying to initiate copy....");
            blockBlob.StartCopyFromBlob(new Uri(amazonObjectUrl), null, null, null);
            Console.WriteLine("Copy started....");
            Console.WriteLine("Now tracking blob's copy progress....");
            DateTime startTime = DateTime.UtcNow;
            bool continueLoop = true;
            while (continueLoop)
            {
                Console.WriteLine("");
                Console.WriteLine("Fetching lists of blobs in Azure blob container....");
                IEnumerable<IListBlobItem> blobsList = blobContainer.ListBlobs(null, true, BlobListingDetails.Copy);
                foreach (var blob in blobsList)
                {
                    var tempBlockBlob = (CloudBlockBlob)blob;
                    var destBlob = blob as CloudBlockBlob;
                    if (tempBlockBlob.Name == azureBlobName)
                    {
                        var copyStatus = tempBlockBlob.CopyState;
                        if (copyStatus != null)
                        {

                            Console.WriteLine("Status of blob copy...." + copyStatus.Status);
                            float percentComplete = 100 *
                                float.Parse(copyStatus.BytesCopied.ToString()) /
                                float.Parse(copyStatus.TotalBytes.ToString());
                            Console.WriteLine("Total bytes to copy...." + copyStatus.TotalBytes);
                            Console.WriteLine("Total bytes copied....." + copyStatus.BytesCopied);
                            Console.WriteLine("Perc. byte copied......{0:N1}", percentComplete);
                            if (copyStatus.Status != CopyStatus.Pending)
                            {

                                continueLoop = false;

                            }
                        }
                    }
                }
                Console.WriteLine("");
                Console.WriteLine("==============================================");
                System.Threading.Thread.Sleep(1000);
            }
            DateTime endTime = DateTime.UtcNow;
            TimeSpan diffTime = endTime - startTime;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("time transfer (D HH:mm:ss):  " +
                  diffTime.Days + " " + diffTime.Hours + ":" +
                  diffTime.Minutes + ":" + diffTime.Seconds);
            Console.ResetColor();
            string url = blockBlob.Uri.ToString();
            Console.WriteLine(url);
            Console.WriteLine("Press any key to terminate the program....");
            Console.ReadLine();

        }

    }
}
