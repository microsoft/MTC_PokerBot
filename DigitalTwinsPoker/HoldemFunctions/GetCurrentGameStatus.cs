using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Azure.DigitalTwins.Core;
using DigitalTwinLibrary;
using System.Collections.Generic;
using System.Text.Json;
using DataTransfer;
using CardDataAndMath;

namespace HoldemFunctions
{
    public static class GetCurrentGameStatus
    {
        [FunctionName("GetCurrentGameStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("Requesting game status");

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

            BasicDigitalTwin playerHandTwin, dealerHandTwin, communityHandTwin;
            List<BasicDigitalTwin> playerCards, dealerCards, communityCards;

            BasicDigitalTwin gameTwin = await DigitalTwinsLibrary.GetGame(dtclient);
            (playerHandTwin, dealerHandTwin, communityHandTwin) = await DigitalTwinsLibrary.GetHands(dtclient, playerName);
            (playerCards, dealerCards, communityCards) = await DigitalTwinsLibrary.GetCards(dtclient, playerHandTwin, dealerHandTwin, communityHandTwin);

            GameStatusTransfer gameStatus = CreateTransferJson(playerName, playerHandTwin, dealerHandTwin, gameTwin, playerCards, dealerCards, communityCards, 
                configuration["CardImageUrl"], configuration["CardImageUrlSAS"]);
            TransferJson.CreateHandsDisplay(ref gameStatus);

            return new OkObjectResult(gameStatus);

        }

        private static GameStatusTransfer CreateTransferJson(string playerName, BasicDigitalTwin playerHandTwin, BasicDigitalTwin dealerHandTwin, BasicDigitalTwin gameTwin,
            List<BasicDigitalTwin> playerCards, List<BasicDigitalTwin> dealerCards, List<BasicDigitalTwin> communityCards, string cardImageUrl, string cardImageUrlSAS)
        {
            GameStatusTransfer gameStatus = new GameStatusTransfer();
            gameStatus.Game = new Game();
            gameStatus.Player = new PlayerHand();
            gameStatus.Dealer = new PlayerHand();
            gameStatus.Community = new CommunityHand();
            gameStatus.Display = new HandsDisplay();

            gameStatus.Game.PotentialWinner = ((JsonElement)gameTwin.Contents["PotentialWinner"]).ToString();
            gameStatus.Game.ProbabilityOfWinning = ((JsonElement)gameTwin.Contents["ProbabilityOfWinning"]).ToString();

            gameStatus.Player.Name = playerName;
            gameStatus.Player.ProbabilityOfWinning = ((JsonElement)playerHandTwin.Contents["ProbabilityOfWinning"]).ToString();
            gameStatus.Player.HandType = ((JsonElement)playerHandTwin.Contents["HandType"]).ToString();
            gameStatus.Player.HighCard = ((JsonElement)playerHandTwin.Contents["HighCard"]).ToString();
            gameStatus.Player.SecondHighCard = ((JsonElement)playerHandTwin.Contents["SecondHighCard"]).ToString();
            gameStatus.Player.ThirdHighCard = ((JsonElement)playerHandTwin.Contents["ThirdHighCard"]).ToString();
            gameStatus.Player.FourthHighCard = ((JsonElement)playerHandTwin.Contents["FourthHighCard"]).ToString();
            gameStatus.Player.FifthHighCard = ((JsonElement)playerHandTwin.Contents["FifthHighCard"]).ToString();
            string[] p = ((JsonElement)playerHandTwin.Contents["Potentials"]).ToString().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (p == null || (p.Length == 1 && p[0] == "None"))
                gameStatus.Player.Potentials = new string[0];
            else
                gameStatus.Player.Potentials = new string[p.Length];
            for (int i = 0; i < gameStatus.Player.Potentials.Length; i++)
            {
                gameStatus.Player.Potentials[i] = p[i];
            }
            if (playerCards == null)
                gameStatus.Player.Cards = new DtCard[0];
            else
                gameStatus.Player.Cards = new DtCard[playerCards.Count];

            // we're going to create a hand so the cards are sorted on return
            // the digital twin model does contain the ordering, but it's combined into a hand
            // so this is simpler, as here we want the cards orders single
            Hand orderHand = new Hand();
            for (int i = 0; i < playerCards.Count; i++)
            {
                orderHand.Add(new Card(((JsonElement)playerCards[i].Contents["Value"]).ToString(), ((JsonElement)playerCards[i].Contents["Suite"]).ToString()));
            }
            for (int i = 0; i < playerCards.Count; i++)
            {
                gameStatus.Player.Cards[i] = new DtCard()
                {
                    Value = orderHand[i].Value,
                    Suite = orderHand[i].Suite,
                    Name = orderHand[i].Name,
                    ImageUrl = $"{cardImageUrl}{orderHand[i].Name}.png?{cardImageUrlSAS}"
                };
            }

            gameStatus.Dealer.ProbabilityOfWinning = ((JsonElement)dealerHandTwin.Contents["ProbabilityOfWinning"]).ToString();
            gameStatus.Dealer.HandType = ((JsonElement)dealerHandTwin.Contents["HandType"]).ToString();
            gameStatus.Dealer.HighCard = ((JsonElement)dealerHandTwin.Contents["HighCard"]).ToString();
            gameStatus.Dealer.SecondHighCard = ((JsonElement)dealerHandTwin.Contents["SecondHighCard"]).ToString();
            gameStatus.Dealer.ThirdHighCard = ((JsonElement)dealerHandTwin.Contents["ThirdHighCard"]).ToString();
            gameStatus.Dealer.FourthHighCard = ((JsonElement)dealerHandTwin.Contents["FourthHighCard"]).ToString();
            gameStatus.Dealer.FifthHighCard = ((JsonElement)dealerHandTwin.Contents["FifthHighCard"]).ToString();
            string[] pp = ((JsonElement)dealerHandTwin.Contents["Potentials"]).ToString().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (pp == null || (pp.Length == 1 && pp[0] == "None"))
                gameStatus.Dealer.Potentials = new string[0];
            else
                gameStatus.Dealer.Potentials = new string[pp.Length];
            for (int i = 0; i < gameStatus.Dealer.Potentials.Length; i++)
            {
                gameStatus.Dealer.Potentials[i] = pp[i];
            }
            if (dealerCards == null)
                gameStatus.Dealer.Cards = new DtCard[0];
            else
                gameStatus.Dealer.Cards = new DtCard[dealerCards.Count];

            orderHand = new Hand();
            for (int i = 0; i < dealerCards.Count; i++)
            {
                orderHand.Add(new Card(((JsonElement)dealerCards[i].Contents["Value"]).ToString(), ((JsonElement)dealerCards[i].Contents["Suite"]).ToString()));
            }
            for (int i = 0; i < dealerCards.Count; i++)
            {
                gameStatus.Dealer.Cards[i] = new DtCard()
                {
                    Value = orderHand[i].Value,
                    Suite = orderHand[i].Suite,
                    Name = orderHand[i].Name,
                    ImageUrl = $"{cardImageUrl}{orderHand[i].Name}.png?{cardImageUrlSAS}"
                };
            }

            if (communityCards == null)
                gameStatus.Community.Cards = new DtCard[0];
            else
                gameStatus.Community.Cards = new DtCard[communityCards.Count];

            orderHand = new Hand();
            for (int i = 0; i < communityCards.Count; i++)
            {
                orderHand.Add(new Card(((JsonElement)communityCards[i].Contents["Value"]).ToString(), ((JsonElement)communityCards[i].Contents["Suite"]).ToString()));
            }
            for (int i = 0; i < communityCards.Count; i++)
            {
                gameStatus.Community.Cards[i] = new DtCard()
                {
                    Value = orderHand[i].Value,
                    Suite = orderHand[i].Suite,
                    Name = orderHand[i].Name,
                    ImageUrl = $"{cardImageUrl}{orderHand[i].Name}.png?{cardImageUrlSAS}"
                };
            }

            return gameStatus;
        }
    }
}
