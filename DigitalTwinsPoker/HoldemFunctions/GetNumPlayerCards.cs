using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.DigitalTwins.Core;
using DigitalTwinLibrary;
using Microsoft.Extensions.Configuration;

namespace HoldemFunctions
{
    public static class GetNumPlayerCards
    {
        [FunctionName("GetNumPlayerCards")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("Requesting the number of player cards");

            var configuration = new ConfigurationBuilder()
              .SetBasePath(context.FunctionAppDirectory)
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string playerName = data?.playerName;
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Player";
            // spaces in id's are bad
            playerName = playerName.Replace(' ', '_');

            DigitalTwinsClient dtclient = DigitalTwinsLibrary.DigitalTwinsLogin(configuration["DIGITALTWINSURL"], configuration["MANAGED_CLIENT_ID"]);

            // get player hand
            BasicDigitalTwin playerHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(playerName);
            if (playerHandTwin == null)
            {
                string res = "The player hand was not found";
                return new NotFoundObjectResult(res);
            }

            int cardsInHand = await DigitalTwinsLibrary.GetNumberOfRelationships(dtclient, playerHandTwin, "has_cards");

            return new OkObjectResult(cardsInHand);
        }
    }
}
