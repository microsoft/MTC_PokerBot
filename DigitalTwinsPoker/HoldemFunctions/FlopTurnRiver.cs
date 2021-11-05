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
using CardDataAndMath;
using System.Collections.Generic;
using System.Text.Json;
using DigitalTwinLibrary;
using System.Text;

namespace HoldemFunctions
{
    public static class FlopTurnRiver
    {
        [FunctionName("FlopRiverTurn")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            var configuration = new ConfigurationBuilder()
              .SetBasePath(context.FunctionAppDirectory)
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();

            // Log in to DT
            DigitalTwinsClient dtclient = DigitalTwinsLibrary.DigitalTwinsLogin(configuration["DIGITALTWINSURL"], configuration["MANAGED_CLIENT_ID"]);

            BasicDigitalTwin playerHandTwin, dealerHandTwin, communityHandTwin, discardHandTwin, deckTwin, gameTwin;
            gameTwin = await DigitalTwinsLibrary.GetGame(dtclient);
            (playerHandTwin, dealerHandTwin, communityHandTwin, discardHandTwin, deckTwin) = await DigitalTwinsLibrary.GetAllHandsAndDeck(dtclient, ((JsonElement)gameTwin.Contents["PlayerName"]).ToString());

            // get the number of cards in the community hand
            int numCardsIncCommunityHand = await DigitalTwinsLibrary.GetNumberOfRelationships(dtclient, communityHandTwin, "has_cards");
            // get the number of cards in the community hand
            int numCardsInDiscardHand = await DigitalTwinsLibrary.GetNumberOfRelationships(dtclient, discardHandTwin, "has_cards");

            Hand playerHand = new Hand();
            Hand dealerHand = new Hand();

            List<BasicDigitalTwin> playerCards, dealerCards, communityCards;
            (playerCards, dealerCards, communityCards) = await DigitalTwinsLibrary.GetCards(dtclient, playerHandTwin, dealerHandTwin, communityHandTwin);

            foreach (var t in playerCards)
            {
                playerHand.Add(Card.CreateName(((JsonElement)t.Contents["Value"]).ToString(), ((JsonElement)t.Contents["Suite"]).ToString()));
            }

            foreach (var t in dealerCards)
            {
                dealerHand.Add(Card.CreateName(((JsonElement)t.Contents["Value"]).ToString(), ((JsonElement)t.Contents["Suite"]).ToString()));
            }

            foreach (var t in communityCards)
            {
                playerHand.Add(Card.CreateName(((JsonElement)t.Contents["Value"]).ToString(), ((JsonElement)t.Contents["Suite"]).ToString()), true);
                dealerHand.Add(Card.CreateName(((JsonElement)t.Contents["Value"]).ToString(), ((JsonElement)t.Contents["Suite"]).ToString()), true);
            }

             int topOfOrder = 0;
            string cardName;
            try
            {
                switch (numCardsIncCommunityHand)
                {
                    // flop
                    case 0:
                        log.LogInformation("Dealing flop");
                        // burn a card
                        (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, discardHandTwin, topOfOrder, numCardsInDiscardHand++, "has_cards");

                        for (int i = 0; i < 3; ++i)
                        {
                            (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, communityHandTwin, topOfOrder, i, "has_cards");
                            playerHand.Add(cardName, true);
                            dealerHand.Add(cardName, true);
                        }

                        break;
                    // turn
                    case 3:
                        log.LogInformation("Dealing turn");
                        topOfOrder = 3;
                        // burn a card
                        (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, discardHandTwin, topOfOrder, numCardsInDiscardHand++, "has_cards");

                        (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, communityHandTwin, topOfOrder, 3, "has_cards");
                        playerHand.Add(cardName, true);
                        dealerHand.Add(cardName, true);

                        break;
                    // river
                    case 4:
                        log.LogInformation("Dealing river");
                        topOfOrder = 4;
                        // burn a card
                        (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, discardHandTwin, topOfOrder, numCardsInDiscardHand++, "has_cards");

                        (topOfOrder, cardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, communityHandTwin, topOfOrder, 4, "has_cards");
                        playerHand.Add(cardName, true);
                        dealerHand.Add(cardName, true);

                        break;

                    default:
                        string badCardMessage = "All cards already dealt";
                        return new BadRequestObjectResult(badCardMessage);
                }
            }
            catch (Exception x)
            {
                return new BadRequestObjectResult(x.Message);
            }

            var res = CalculateWinner.GetWinner(playerHand, dealerHand, ((JsonElement)gameTwin.Contents["PotentialWinner"]).ToString(),
            float.Parse(((JsonElement)playerHandTwin.Contents["ProbabilityOfWinning"]).ToString()), float.Parse(((JsonElement)dealerHandTwin.Contents["ProbabilityOfWinning"]).ToString()));

            // here add potentials



            await DigitalTwinsLibrary.UpdatePotentials(dtclient, playerHandTwin, CalculatePotentialsString(playerHand.Potentials));
            await DigitalTwinsLibrary.UpdatePotentials(dtclient, dealerHandTwin, CalculatePotentialsString(dealerHand.Potentials));

            await DigitalTwinsLibrary.UpdateProbabilityOfWinningTelemetry(dtclient, playerHandTwin, res.playerPercentage);
            await DigitalTwinsLibrary.UpdateProbabilityOfWinningTelemetry(dtclient, dealerHandTwin, res.dealerPercentage);

            await DigitalTwinsLibrary.UpdateHandInfo(dtclient, dealerHandTwin, dealerHand);
            await DigitalTwinsLibrary.UpdateHandInfo(dtclient, playerHandTwin, playerHand);

            await DigitalTwinsLibrary.UpdateGameTelemetry(dtclient, gameTwin, res.winnerPercentage, res.dealerWinner);

            string responseMessage = "Success";
            return new OkObjectResult(responseMessage);
        }

        private static string CalculatePotentialsString((int threeOfAKind, int straight, int flush, int fullHouse, int fourOfAKind, int straightFlush) potentials)
        {
            StringBuilder sb = new StringBuilder();
            if (potentials.threeOfAKind != 0) sb.Append("ThreeOfAKind");
            if (potentials.straight != 0)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append("Straight");
            }
            if (potentials.flush != 0)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append("Flush");
            }
            if (potentials.fullHouse != 0)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append("FullHouse");
            }
            if (potentials.fourOfAKind != 0)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append("FourOfAKind");
            }
            if (potentials.straightFlush != 0)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append("StraightFlush");
            }

            return sb.ToString();
        }
    }
}
