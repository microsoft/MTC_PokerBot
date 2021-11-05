using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Azure;
using System.Text;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using CardDataAndMath;
using System.Collections.Generic;
using System.Text.Json;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;

namespace Percept_Functions
{
    public static class HandlePlayerCard
    {
        static readonly bool onlyOCR = false;

        [FunctionName("HandlePlayerCard")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("Received a batch from streaming analytics");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            BlobContainerClient blobContainerClient = new BlobContainerClient(configuration["GAMESTORAGECONNECTIONSTRING"], configuration["LockFileContainer"]);
            try
            {
                BlobClient lockBlobClient = blobContainerClient.GetBlobClient(configuration["LockFile"]);
                Stream dis = lockBlobClient.OpenRead();
                dis.Close();
            }
            catch (RequestFailedException)
            {
                CreateLockfile(configuration);
            }

            log.LogInformation($"Number of records received by function = {data.Count}");

            string cardText = string.Empty;

            ComputerVisionClient cvClient =
                new ComputerVisionClient(new ApiKeyServiceClientCredentials(configuration["COMPUTERVISIONSUBSCRIPTIONKEY"]))
                { Endpoint = configuration["COMPUTERVISIONENDPOINT"] };

            FormRecognizerClient frClient = new FormRecognizerClient(new Uri(configuration["FORMRECOGNIZERENDPOINT"]),
                new AzureKeyCredential(configuration["FORMRECOGNIZERSUBSCRIPTIONKEY"]));

            bool leases = bool.Parse(configuration["leases"]);

            BlobClient blobClient = blobContainerClient.GetBlobClient(configuration["LockFile"]);
            BlobLeaseClient blobLeaseClient = blobClient.GetBlobLeaseClient();
            bool attemptedML = false;
            bool attemptedImageRead = false;
            try
            {
                BlobLease blobLease;
                if (leases == true)
                    blobLease = blobLeaseClient.Acquire(TimeSpan.FromSeconds(-1));

                attemptedImageRead = true;
                if (ImageInput(configuration, out MemoryStream imageStream, out MemoryStream imageStream2) == true)
                {
                    attemptedML = true;

                    // we seem to get the best results by trying form recognition first.  It seems spot on for the non picture cards every time
                    // and we don't have the 6/9 problem.  If we don't get a result, we look at OCR, and now it will look at both corners.  It 
                    // seems to work better than only using one.  I did double the memory requirement.....
                    if(onlyOCR == false)
                        cardText = await PerformFR(frClient, imageStream);
                    if (string.IsNullOrWhiteSpace(cardText))
                    {
                        cardText = await PerformOCR(cvClient, imageStream2, configuration["69line"]);
                    }
                }
                else
                {
                    throw new Exception("Unable to read in the frame image");
                }
            }
            catch (RequestFailedException x)
            {
                log.LogInformation($"Unable to acquire file lock lease: {x.Message}");
                return new BadRequestObjectResult($"Unable to acquire file lock lease: {x.Message}");

                throw;
            }
            catch (Exception x)
            {
                if (attemptedImageRead == false && attemptedML == false)
                {
                    log.LogInformation($"Unable to acquire file lock lease: {x.Message}");
                    return new BadRequestObjectResult($"Unable to acquire file lock lease: {x.Message}");
                }
                else if (attemptedML == false)
                {
                    log.LogInformation($"Unable to read image from blob storage: {x.Message}");
                    return new BadRequestObjectResult($"Unable to read image from blob storage: {x.Message}");
                }
                else
                {
                    log.LogInformation($"Card unable to be read from FR or OCR: {x.Message}");
                    return new BadRequestObjectResult($"Card unable to be read from FR or OCR: {x.Message}");
                }
            }
            finally
            {
                if (leases == true)
                    blobLeaseClient.Release();
            }

            //string responseMessage = "OK";
            //if we could not read the card, we'll just bail and assume that
            // we only have low confidence numbers for everything, or we just could not get a lease and there are
            // now too many functions waiting, either way, just end without an alarm
            if (string.IsNullOrWhiteSpace(cardText))
            {
                log.LogInformation("Card unable to be read");
                return new OkObjectResult("Card unable to be read");
            }

            try
            {
                DeleteImage(configuration);
            }
            catch
            {
                // sometimes the blob image will be replaced before we can delete it
                // so just ignore...
            }
            string highConfidenceLabel = string.Empty;
            float highConfidenceConfidence = 0.0F;
            for (var i = 0; i < data.Count; i++)
            {
                if (data[i].Confidence > highConfidenceConfidence)
                {
                    string c = data[i].Confidence;
                    highConfidenceConfidence = float.Parse(c);
                    highConfidenceLabel = data[i].Label;
                    highConfidenceLabel = highConfidenceLabel.ToUpperInvariant();
                }
                //string label = data[i].Label;
                //string confidence = data[i].Confidence;
                //string timeStamp = data[i].TimeStamp;
                //log.LogInformation($"Received from Streaming Analytics {label} {confidence} {timeStamp}");
            }

            // we'll have to see if this is a good number
            if (highConfidenceConfidence < 0.2)
            {
                log.LogInformation("Low confidence, card not sent to game");

                // if we are really low confidence, stop
                return new OkObjectResult("Low confidence, card not sent to game");
            }

            // this will throw an error if it fails
            Card card = new Card(cardText + highConfidenceLabel);

            await CallPlayerCardFunction(configuration, card);

            log.LogInformation(card.Name);
            return new OkObjectResult(card.Name);

        }

        private static void DeleteImage(IConfigurationRoot configuration)
        {
            BlobContainerClient imageBlobContainerClient = new BlobContainerClient(configuration["GAMESTORAGECONNECTIONSTRING"], configuration["FrameContainer"]);
            BlobClient imageBlobClient = imageBlobContainerClient.GetBlobClient(configuration["FrameFile"]);
            imageBlobClient.Delete(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
        }
        private static bool ImageInput(IConfigurationRoot configuration, out MemoryStream imageStream, out MemoryStream imageStream2)
        {
            imageStream = null;
            imageStream2 = null;

            try
            {
                imageStream = ReadImage(configuration);
                imageStream2 = new MemoryStream();
                imageStream.CopyTo(imageStream2);
                imageStream.Position = 0;
                imageStream2.Position = 0;        
            }
            catch(RequestFailedException x)
            {
                if (x.Message.StartsWith("The specified blob does not exist"))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }

        private static async Task CallPlayerCardFunction(IConfigurationRoot configuration, Card card)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{configuration["PlayerCardUrl"]}");
            request.Content = new StringContent($"{{\"card\":\"{card.Name}\"}}",
                                Encoding.UTF8,
                                "application/json");
            HttpClient hClient = new HttpClient();
            await hClient.SendAsync(request);
        }


        private static async Task<string> PerformFR(FormRecognizerClient cvClient, MemoryStream imageStream)
        {
            string cardText = string.Empty;

            Response<FormPageCollection> response = await cvClient.StartRecognizeContentAsync(imageStream).WaitForCompletionAsync();
            FormPageCollection formPages = response.Value;

            // we only have one page
            if (formPages.Count > 0)
            {
                // we only care about the first line
                if (formPages[0].Lines.Count > 0)
                {
                    // we only care about the first work
                    if (formPages[0].Lines[0].Words.Count > 0)
                    {
                        cardText = formPages[0].Lines[0].Words[0].Text;
                    }
                }
            }

            return AcceptText(cardText);
        }

        private static async Task<string> PerformOCR(ComputerVisionClient cvClient, MemoryStream imageStream, string sixNineLine)
        {
            string cardText = string.Empty;
            ReadInStreamHeaders textHeaders = await cvClient.ReadInStreamAsync(imageStream);
            string operationLocation = textHeaders.OperationLocation;
            string operationId = operationLocation[^36..];
            ReadOperationResult results;
            int failureCounter = 50;

            // let's try to speed it up by having the waits continually get smaller
            int sleepTime = 2000; // the first sleep will be half of this
            do
            {
                results = await cvClient.GetReadResultAsync(Guid.Parse(operationId));
                if (results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted)
                {
                    sleepTime = sleepTime / 2;
                    if (sleepTime < 75) sleepTime = 75;
                    Thread.Sleep(sleepTime);
                    failureCounter--;
                }
            }
            while ((results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted) && failureCounter > 0);

            // don't run forever, this means that cog services did not respond in 50 seconds
            if(failureCounter <= 0)
            {
                throw new Exception("No return from CogServices OCR");
            }

            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    if(!string.IsNullOrWhiteSpace(AcceptText(line.Text)))
                    {
                        if(AcceptText(line.Text) == "01")
                        {
                            cardText = "10";
                            break;
                        }
                        else if(AcceptText(line.Text) != "9" && AcceptText(line.Text) != "6")
                        {
                            cardText = AcceptText(line.Text);
                            break;
                        }
                        else
                        {
                            double? highest = line.BoundingBox[1];
                            if (line.BoundingBox[3] < highest)
                                highest = line.BoundingBox[3];
                            // here, if the number is in the bottom 30% of the screen, we will use it and inverse it
                            // we usually do not get the top number at all, but we know the bottom is upside down
                            if (highest > page.Height * float.Parse(sixNineLine))
                            {
                                if (AcceptText(line.Text) == "6")
                                    cardText = "9";
                                else
                                    cardText = "6";
                            }
                            else
                            {
                                cardText = AcceptText(line.Text);
                            }
                            break;
                        }
                    }
                }
            }

            return cardText;
        }

        private static MemoryStream ReadImage(IConfigurationRoot configuration)
        {
            BlobContainerClient imageBlobContainerClient = new BlobContainerClient(configuration["GAMESTORAGECONNECTIONSTRING"], configuration["FrameContainer"]);
            BlobClient imageBlobClient = imageBlobContainerClient.GetBlobClient(configuration["FrameFile"]);
            MemoryStream imageStream = new MemoryStream();
            _ = imageBlobClient.DownloadTo(imageStream);

            imageStream.Position = 0;
            return imageStream;
        }

        private static string AcceptText(string txt)
        {
            string cardText;

            // we only accept one word
            switch (txt.ToUpperInvariant())
            {
                case "A":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                case "10":
                case "J":
                case "Q":
                case "K":
                    cardText = txt.ToUpperInvariant();
                    break;
                default:
                    cardText = string.Empty;
                    break;
            }

            return cardText;
        }




        private static void CreateLockfile(IConfigurationRoot configuration)
        {
            var blobClient = new BlobClient(configuration["GAMESTORAGECONNECTIONSTRING"], configuration["LockFileContainer"], configuration["LockFile"]);
            using var stream = new MemoryStream();
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 8, true))
                {
                    writer.WriteLine("lockme");
                }
                stream.Position = 0;
                blobClient.Upload(stream);
            }
        }
    }
}
