using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CardDataAndMath;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using DigitalTwinLibrary;

namespace HoldemFunctions
{
    public static class PlayerCard
    {
        [FunctionName("PlayerCard")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {

            var configuration = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            //log.LogInformation("User has been dealt a card");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string cardName = data?.card;

            Card pcard = null;
            try
            {
                pcard = new Card(cardName);
            }
            catch
            {
                string badCardMessage = "The submitted card is not valid, card names should be similar to 3S, KH";
                return new BadRequestObjectResult(badCardMessage);
            }

            cardName = cardName.ToUpperInvariant();
            //log.LogInformation($"User has been dealt a {cardName}");

            // Log in to DT
            DigitalTwinsClient dtclient = DigitalTwinsLibrary.DigitalTwinsLogin(configuration["DIGITALTWINSURL"], configuration["MANAGED_CLIENT_ID"]);


            // get game, there is only one
            BasicDigitalTwin gameTwin = await DigitalTwinsLibrary.GetGame(dtclient);
            if (gameTwin == null)
            {
                string badCardMessage = "The game was not found";
                return new BadRequestObjectResult(badCardMessage);
            }

            BasicDigitalTwin playerHandTwin, dealerHandTwin, deckTwin;
            (playerHandTwin, dealerHandTwin, _, _, deckTwin) = await DigitalTwinsLibrary.GetAllHandsAndDeck(dtclient, ((JsonElement)gameTwin.Contents["PlayerName"]).ToString());

            // get the number of cards in the hand
            int numCardsInHand = await DigitalTwinsLibrary.GetNumberOfRelationships(dtclient, playerHandTwin, "has_cards");
            // if we already have two, we are done
            if (numCardsInHand >= 2)
            {
                string responseMessageNumCard = "Two player cards have already been dealt";
                return new BadRequestObjectResult(responseMessageNumCard);
            }

            Hand playerHand = null;
            Hand dealerHand = null;

            if (numCardsInHand > 0)
            {
                playerHand = new Hand();
                dealerHand = new Hand();

                // if this is the second card, grab the first card
                BasicDigitalTwin firstCard = await DigitalTwinsLibrary.GetFirstCard(dtclient, playerHandTwin);
                if (firstCard == null)
                {
                    string badCardMessage = $"Unable to find the first card in the player hand";
                    return new BadRequestObjectResult(badCardMessage);  
                }
                playerHand.Add(Card.CreateName(((JsonElement)firstCard.Contents["Value"]).ToString(), ((JsonElement)firstCard.Contents["Suite"]).ToString()));
            }

            // get player card, we can query here as the deck never changes so latency is not an issue
            BasicDigitalTwin playerCardTwin = await DigitalTwinsLibrary.GetOneTwin(dtclient, $"SELECT * FROM DIGITALTWINS WHERE IS_OF_MODEL('dtmi:games:Card;1') and Value = '{pcard.Value}' and Suite = '{pcard.Suite}'");
            if (playerCardTwin == null)
            {
                string badCardMessage = $"The submitted card {cardName} was not found in the deck";
                return new BadRequestObjectResult(badCardMessage);
            }
            if (numCardsInHand > 0)
            {
                playerHand.Add(Card.CreateName(((JsonElement)playerCardTwin.Contents["Value"]).ToString(), ((JsonElement)playerCardTwin.Contents["Suite"]).ToString()));
            }

            // take the card from the deck
            try
            {
                await dtclient.DeleteRelationshipAsync(deckTwin.Id, $"{deckTwin.Id}-has-{playerCardTwin.Id}");
            }
            catch
            {
                string badCardMessage = $"The player submitted the same card twice";
                return new BadRequestObjectResult(badCardMessage);
            }

            // add the card to the player hand
            string handToCardRel = $"{playerHandTwin.Id}-has-{playerCardTwin.Id}";
            await dtclient.CreateOrReplaceRelationshipAsync(playerHandTwin.Id, handToCardRel, new BasicRelationship { TargetId = playerCardTwin.Id, Name = "has_cards", Properties = { { "cardOrder", numCardsInHand } } });

            // if this is the first card, we are done
            if (numCardsInHand == 0)
            {
                string responseMessageNumCard = "Success";
                return new OkObjectResult(responseMessageNumCard);
            }

            // deal the top two cards to the dealer
            int topOfOrder = 0;
            string movedCardName;
            try
            {
                for (int i = 0; i < 2; ++i)
                {
                    (topOfOrder, movedCardName) = await DigitalTwinsLibrary.MoveTopDeckCardToHand(dtclient, deckTwin, dealerHandTwin, topOfOrder, i, "has_cards");
                    dealerHand.Add(movedCardName);
                }
            }
            catch (Exception x)
            {
                return new BadRequestObjectResult(x.Message);
            }

            var res = CalculateWinner.GetWinner(playerHand, dealerHand);

            await DigitalTwinsLibrary.UpdateProbabilityOfWinningTelemetry(dtclient, playerHandTwin, res.playerPercentage);
            await DigitalTwinsLibrary.UpdateProbabilityOfWinningTelemetry(dtclient, dealerHandTwin, res.dealerPercentage);

            await DigitalTwinsLibrary.UpdateHandInfo(dtclient, dealerHandTwin, dealerHand);
            await DigitalTwinsLibrary.UpdateHandInfo(dtclient, playerHandTwin, playerHand);

            await DigitalTwinsLibrary.UpdateGameTelemetry(dtclient, gameTwin, res.winnerPercentage, res.dealerWinner);

            string responseMessage = "Success";
            return new OkObjectResult(responseMessage);
        }


    }
}
