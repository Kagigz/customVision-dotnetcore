using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using System.Threading.Tasks;

namespace CustomVision
{
    public class AzureStorageHelpers
    {

        // Replace your account name here
        private static readonly string ACCOUNT_NAME = "YOUR_ACCOUNT_NAME";
        // Replace your account key here
        private static readonly string ACCOUNT_KEY = "YOUR_ACCOUNT_KEY";
        private static readonly string CONNECTION_STRING = $"DefaultEndpointsProtocol=https;AccountName={ACCOUNT_NAME};AccountKey={ACCOUNT_KEY}";
        // Replace your fileshare name here
        private static readonly string SHARE_NAME = "fileshare";
        // Replace your SAS token here
        private static readonly string SAS_TOKEN = "YOUR_SAS_TOKEN";

        public static CloudFileShare Setup()
        {
            Console.WriteLine("Setting up File Share Connection...");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CONNECTION_STRING);
            // Create a CloudFileClient object for credentialed access to Azure Files.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            // Get a reference to the file share we created previously.
            CloudFileShare share = fileClient.GetShareReference(SHARE_NAME);

            // Ensure that the share exists.
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                Console.WriteLine($"\nConnection to File Share succeeded. Root Dir storage URI: {rootDir.StorageUri.PrimaryUri}");

                return share;
            }

            return null;
        }

        // Assumes the images are in subfolders at the root of the fileshare
        public static Uri GetFileURL(string path, CloudFileShare share)
        {

            CloudFileDirectory rootDir = null;
            CloudFileDirectory dir = null;
            CloudFile file = null;

            

            string[] splitPath = path.Split('/');

            try
            {
                rootDir = share.GetRootDirectoryReference();
                dir = rootDir.GetDirectoryReference(splitPath[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n{e.GetType().Name}: Error accessing directory.");
                throw;
            }

            if (dir != null)
            {
                try
                {
                    file = dir.GetFileReference(splitPath[1]);

                    if (file.Exists())
                    {

                        Console.WriteLine($"File '{file.Name}' successfully retrieved.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e.GetType().Name}: File '{file.Name}' could not be retrieved.");
                    throw;
                }
            }

            Uri URI;
            Uri.TryCreate(file.Uri.ToString() + SAS_TOKEN,UriKind.Absolute,out URI);

            return URI;
        }



    }
}
