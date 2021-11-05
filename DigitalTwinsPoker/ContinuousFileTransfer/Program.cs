using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace ContinuousFileTransfer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .AddCommandLine(args)
              .Build();

            string lastHashString = string.Empty; ;
            SHA256 sha256 = SHA256.Create();

            while (true)
            { 
                DateTime then = DateTime.Now;
                try
                {
                    // The percept is way variable so this only really comes into effect if the camera gets turned off.  Still worth it
                    string hashString;
                    using (var stream = File.OpenRead(configuration["FileToUpload"]))
                    {
                        hashString  = Convert.ToBase64String(sha256.ComputeHash(stream));
                    }

                    if (hashString != lastHashString)
                    {
                        lastHashString = hashString;

                        BlobContainerClient blobContainerClient = new BlobContainerClient(configuration["GAMESTORAGECONNECTIONSTRING"], configuration["ContainerName"]);
                        Console.WriteLine($"Uploading {configuration["BlobName"]} at {then.ToShortTimeString()}");
                        BlobClient blobClient = blobContainerClient.GetBlobClient(configuration["BlobName"]);
                        await blobClient.UploadAsync(configuration["FileToUpload"], new BlobUploadOptions());
                    }
                }
                catch (Exception x)
                {
                    Console.WriteLine($"Error, no upload, {x.Message}");
                }

                TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - then.Ticks);
                int miliseconds = int.Parse(configuration["RefreshMS"]) - ts.Milliseconds;
                if(miliseconds > 0)
                {
                    Console.WriteLine($"Sleeping for {miliseconds} miliseconds");
                    Thread.Sleep(miliseconds);
                }
            }

        }
    }

}
