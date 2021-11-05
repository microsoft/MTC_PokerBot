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
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Azure;
using CardDataAndMath;
using DigitalTwinLibrary;

namespace HoldemFunctions
{
    public static class CreateGame
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("CreateGame")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("Request to start a new game of Texas Hold 'Em");

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

            string gameType = data?.gameType;
            if (string.IsNullOrWhiteSpace(gameType) || gameType != "Manual")
                gameType = "Camera";

            // Log in to DT
            DigitalTwinsClient dtclient = DigitalTwinsLibrary.DigitalTwinsLogin(configuration["DIGITALTWINSURL"], configuration["MANAGED_CLIENT_ID"]);

            // Clean out old game
            await DeleteTwins(dtclient, "dtmi:games:Game;1");
            await DeleteTwins(dtclient, "dtmi:games:Deck;1");
            await DeleteTwins(dtclient, "dtmi:games:Hand;1");
            await DeleteTwins(dtclient, "dtmi:games:Card;1");

            BlobContainerClient blobContainerClient = new BlobContainerClient(configuration["GAMESTORAGECONNECTIONSTRING"], "models");

            // add models if they are not already in the DT
            AsyncPageable<DigitalTwinsModelData> modelDataList = await AddModels(dtclient, blobContainerClient);

            // create twins for game
            // game twin
            string gameTwinId = await CreateGameTwin(dtclient, modelDataList, "Texas Hold'Em", playerName, gameType);

            // deck twin
            string deckTwinId = await CreateDeckTwin(dtclient, modelDataList, gameTwinId, "Deck");

            // hand twin player
            await CreateHandTwin(dtclient, modelDataList, gameTwinId, playerName);

            // hand twin dealer
            await CreateHandTwin(dtclient, modelDataList, gameTwinId, "Dealer");

            // hand twin dealer
            await CreateHandTwin(dtclient, modelDataList, gameTwinId, "Community");

            // hand twin dealer
            await CreateHandTwin(dtclient, modelDataList, gameTwinId, "Discard");

            // cards
            Deck deckRepresentation = new Deck();
            deckRepresentation.CreateNewDeck();
            deckRepresentation.ShuffleDeck();

            foreach (var cardw in deckRepresentation)
            {
                await CreateCardTwin(dtclient, modelDataList, gameTwinId, deckTwinId, cardw);
            }

            log.LogInformation($"Game created for user {playerName}, Deck shuffled");

            string responseMessage = "Success";
            return new OkObjectResult(responseMessage);
        }

        private static async Task<string> CreateGameTwin(DigitalTwinsClient dtclient, AsyncPageable<DigitalTwinsModelData> modelDataList, string gameName, string playerName, string gameType)
        {
            var gameData = new BasicDigitalTwin();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                if (md.Id != "dtmi:games:Game;1")
                    continue;

                gameData.Metadata.ModelId = md.Id;
                gameData.Contents.Add("Name", gameName);
                gameData.Contents.Add("ProbabilityOfWinning", 0.0F);
                gameData.Contents.Add("PotentialWinner", "Tie");
                gameData.Contents.Add("PlayerName", playerName);
                gameData.Contents.Add("GameType", gameType);
            }

            string gameTwinId = "Game";
            await dtclient.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(gameTwinId, gameData);

            return gameTwinId;
        }

        private static async Task<string> CreateCardTwin(DigitalTwinsClient dtclient, AsyncPageable<DigitalTwinsModelData> modelDataList, string gameTwinId, string deckTwinId, CardWrapper cardw)
        {
            var cardTwinData = new BasicDigitalTwin();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                if (md.Id != "dtmi:games:Card;1")
                    continue;

                cardTwinData.Metadata.ModelId = md.Id;
                cardTwinData.Contents.Add("Value", cardw.Value);
                cardTwinData.Contents.Add("NumericalValue", cardw.value);
                cardTwinData.Contents.Add("Suite", cardw.Suite);
                cardTwinData.Contents.Add("Color", cardw.Color);
            }

            string cardTwinId = cardw.Order.ToString("D2");
            await dtclient.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(cardTwinId, cardTwinData);

            // deck to card relationship
            string deckToCardRel = $"{deckTwinId}-has-{cardTwinId}";
            await dtclient.CreateOrReplaceRelationshipAsync(deckTwinId, deckToCardRel, new BasicRelationship { TargetId = cardTwinId, Name = "has_cards", Properties = { { "cardOrder", cardw.Order } } });

            return cardTwinId;
        }


        private static async Task<string> CreateDeckTwin(DigitalTwinsClient dtclient, AsyncPageable<DigitalTwinsModelData> modelDataList, string gameTwinId, string deckName)
        {
            var deckData = new BasicDigitalTwin();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                if (md.Id != "dtmi:games:Deck;1")
                    continue;

                deckData.Metadata.ModelId = md.Id;
                deckData.Contents.Add("Name", deckName);
            }

            string deckTwinId = deckName;
            await dtclient.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(deckTwinId, deckData);

            // game to deck relationship
            string gameToDeckRel = $"{gameTwinId}-has-{deckTwinId}";
            await dtclient.CreateOrReplaceRelationshipAsync(gameTwinId, gameToDeckRel, new BasicRelationship { TargetId = deckTwinId, Name = "has_decks" });
            return deckTwinId;
        }

        private static async Task<string> CreateHandTwin(DigitalTwinsClient dtclient, AsyncPageable<DigitalTwinsModelData> modelDataList, string gameTwinId, string handName)
        {
            var handDealerData = new BasicDigitalTwin();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                if (md.Id != "dtmi:games:Hand;1")
                    continue;

                handDealerData.Metadata.ModelId = md.Id;
                handDealerData.Contents.Add("Name", handName);
                handDealerData.Contents.Add("ProbabilityOfWinning", 0.0F);
                handDealerData.Contents.Add("HandType", "No Cards");
                handDealerData.Contents.Add("HighCard", "Zero");
                handDealerData.Contents.Add("SecondHighCard", "Zero");
                handDealerData.Contents.Add("ThirdHighCard", "Zero");
                handDealerData.Contents.Add("FourthHighCard", "Zero");
                handDealerData.Contents.Add("FifthHighCard", "Zero");
                handDealerData.Contents.Add("Potentials", "None");
            }

            string handTwinId = handName;
            await dtclient.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(handTwinId, handDealerData);

            // game to handdealer relationship
            string gameToHandDealerRel = $"{gameTwinId}-has-{handTwinId}";
            await dtclient.CreateOrReplaceRelationshipAsync(gameTwinId, gameToHandDealerRel, new BasicRelationship { TargetId = handTwinId, Name = "has_hands" });

            return handTwinId;
        }

        private static async Task DeleteTwins(DigitalTwinsClient dtclient, string modelname)
        {
            string query = $"SELECT * FROM digitaltwins WHERE IS_OF_MODEL('{modelname}')";
            AsyncPageable<BasicDigitalTwin> queryResult = dtclient.QueryAsync<BasicDigitalTwin>(query);

            await foreach (BasicDigitalTwin twin in queryResult)
            {
                AsyncPageable<BasicRelationship> relationships = dtclient.GetRelationshipsAsync<BasicRelationship>(twin.Id);
                await foreach (var relationship in relationships)
                {
                    await dtclient.DeleteRelationshipAsync(twin.Id, relationship.Id);
                }

                await dtclient.DeleteDigitalTwinAsync(twin.Id);
            }
        }

        private static async Task<AsyncPageable<DigitalTwinsModelData>> AddModels(DigitalTwinsClient dtclient, BlobContainerClient blobContainerClient)
        {
            bool cm = false;
            bool dm = false;
            bool gm = false;
            bool hm = false;

            AsyncPageable<DigitalTwinsModelData> modelDataList = dtclient.GetModelsAsync();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                switch (md.Id)
                {
                    case "dtmi:games:Card;1":
                        cm = true;
                        break;
                    case "dtmi:games:Hand;1":
                        hm = true;
                        break;
                    case "dtmi:games:Game;1":
                        gm = true;
                        break;
                    case "dtmi:games:Deck;1":
                        dm = true;
                        break;
                }
            }

            try
            {
                if (cm == false)
                {
                    await CreateModel(dtclient, blobContainerClient, "Card.json");
                }
            }
            catch (RequestFailedException x)
            {
                if (!x.Message.Contains("already exist")) throw;
            }

            try
            {
                if (dm == false)
                {
                    await CreateModel(dtclient, blobContainerClient, "Deck.json");
                }
            }
            catch (RequestFailedException x)
            {
                if (!x.Message.Contains("already exist")) throw;
            }

            try
            {
                if (gm == false)
                {
                    await CreateModel(dtclient, blobContainerClient, "Game.json");
                }
            }
            catch (RequestFailedException x)
            {
                if (!x.Message.Contains("already exist")) throw;
            }

            try
            {
                if (hm == false)
                {
                    await CreateModel(dtclient, blobContainerClient, "Hand.json");
                }
            }
            catch (RequestFailedException x)
            {
                if (!x.Message.Contains("already exist")) throw;
            }

            return modelDataList;
        }

        private static async Task CreateModel(DigitalTwinsClient dtclient, BlobContainerClient blobContainerClient, string modelFileName)
        {
            string cardmodel = await ReadAllText(blobContainerClient, modelFileName);
            var models = new List<string>() { cardmodel };
            await dtclient.CreateModelsAsync(models);
        }

        public static async Task<string> ReadAllText(BlobContainerClient blobContainerClient, string blobname)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobname);
            using var cardStreamReader = new StreamReader(await blobClient.OpenReadAsync());
            return await cardStreamReader.ReadToEndAsync();
        }
    }
}
