using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using CardDataAndMath;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DigitalTwinLibrary
{
    public class AliasedCBasicDigitalTwin
    {
        [JsonPropertyName("C")]
        public BasicDigitalTwin Twin { get; set; }
    }

    public static class DigitalTwinsLibrary
    {
        public static DigitalTwinsClient DigitalTwinsLogin(string url, string managedClientId)
        {
            var options = new DefaultAzureCredentialOptions();

            if(!string.IsNullOrWhiteSpace(managedClientId))
            {
                options.ManagedIdentityClientId = managedClientId;
            }

            var credential = new DefaultAzureCredential(options);
            var dtclient = new DigitalTwinsClient(new Uri(url), credential);
            return dtclient;
        }

        public static async Task UpdateGameTelemetry(DigitalTwinsClient dtclient, BasicDigitalTwin twin, float winnerPercentage, int dealerWinner)
        {
            var update = new JsonPatchDocument();
            update.AppendReplace("/ProbabilityOfWinning", winnerPercentage);

            if (dealerWinner == 1)
                update.AppendReplace("/PotentialWinner", "Dealer");
            else if (dealerWinner == 0)
                update.AppendReplace("/PotentialWinner", "Player");
            else
                update.AppendReplace("/PotentialWinner", "Tie");
            await dtclient.UpdateDigitalTwinAsync(twin.Id, update);
        }

        public static async Task UpdatePotentials(DigitalTwinsClient dtclient, BasicDigitalTwin twin, string potentials)
        {
            var update = new JsonPatchDocument();
            update.AppendReplace("/Potentials", potentials);
            await dtclient.UpdateDigitalTwinAsync(twin.Id, update);
        }

        public static async Task UpdateProbabilityOfWinningTelemetry(DigitalTwinsClient dtclient, BasicDigitalTwin twin, float percentage)
        {
            var update = new JsonPatchDocument();
            update.AppendReplace("/ProbabilityOfWinning", percentage);
            await dtclient.UpdateDigitalTwinAsync(twin.Id, update);
        }

        public static async Task UpdateHandInfo(DigitalTwinsClient dtclient, BasicDigitalTwin twin, Hand hand)
        {
            var update = new JsonPatchDocument();
            update.AppendReplace("/HandType", Hand.ReverseTargetHands[hand.Description.handType]);
            update.AppendReplace("/HighCard", Card.CardValueNames[hand.Description.highCard]);
            update.AppendReplace("/SecondHighCard", Card.CardValueNames[hand.Description.secondHighCard]);
            update.AppendReplace("/ThirdHighCard", Card.CardValueNames[hand.Description.thirdHighCard]);
            update.AppendReplace("/FourthHighCard", Card.CardValueNames[hand.Description.fourthHighCard]);
            update.AppendReplace("/FifthHighCard", Card.CardValueNames[hand.Description.fifthHighCard]);
            await dtclient.UpdateDigitalTwinAsync(twin.Id, update);
        }

        public static async Task<BasicDigitalTwin> GetOneTwin(DigitalTwinsClient dtclient, string query)
        {
            AsyncPageable<BasicDigitalTwin> twinList = dtclient.QueryAsync<BasicDigitalTwin>(query);
            BasicDigitalTwin twin = null;
            await foreach (var d in twinList)
            {
                twin = d;
            }

            return twin;
        }

        public static async Task<BasicDigitalTwin> GetOneTwinAliasedC(DigitalTwinsClient dtclient, string query)
        {
            AsyncPageable<AliasedCBasicDigitalTwin> twinList = dtclient.QueryAsync<AliasedCBasicDigitalTwin>(query);
            BasicDigitalTwin twin = null;
            await foreach (var d in twinList)
            {
                twin = d.Twin;
                break;
            }

            return twin;
        }

        public static async Task<List<BasicDigitalTwin>> GetTwinListAliasedC(DigitalTwinsClient dtclient, string query)
        {
            List<BasicDigitalTwin> outList = new List<BasicDigitalTwin>();
            AsyncPageable<AliasedCBasicDigitalTwin> twinList = dtclient.QueryAsync<AliasedCBasicDigitalTwin>(query);
            await foreach (var d in twinList)
            {
                outList.Add(d.Twin);
            }

            return outList;
        }

        public static async Task<int> GetNumberOfRelationships(DigitalTwinsClient dtclient, BasicDigitalTwin twin, string relationshipName)
        {
            AsyncPageable<BasicRelationship> rels = dtclient.GetRelationshipsAsync<BasicRelationship>(twin.Id);
            int numRelationships = 0;
            await foreach (var r in rels)
            {
                if (r.Name == relationshipName)
                    ++numRelationships;
            }

            return numRelationships;
        }

        public static async Task<(int topOfDeck, string cardName)> MoveTopDeckCardToHand(DigitalTwinsClient dtclient, BasicDigitalTwin deckTwin, BasicDigitalTwin handTwin,
            int topOfDeck, int iorder, string deletionRelationshipFromDeck)
        {
            BasicDigitalTwin nextCard = null;
            bool foundRelationship = false;

            do
            {
                nextCard = await DigitalTwinsLibrary.GetOneTwin(dtclient, $"SELECT * FROM DIGITALTWINS WHERE IS_OF_MODEL('dtmi:games:Card;1') and $dtId = '{topOfDeck:D2}'");
                if (nextCard == null)
                {
                    string missingCardError = $"We can't find that card in the game (topOfDeck: {topOfDeck})";
                    throw new Exception(missingCardError);
                }
                ++topOfDeck;

                AsyncPageable<BasicRelationship> rels = dtclient.GetRelationshipsAsync<BasicRelationship>(deckTwin.Id);
                await foreach (var r in rels)
                {
                    if (r.Name == deletionRelationshipFromDeck && r.Id == $"{deckTwin.Id}-has-{nextCard.Id}")
                    {
                        foundRelationship = true;
                        break;
                    }
                }

            } while (foundRelationship == false);

            await dtclient.DeleteRelationshipAsync(deckTwin.Id, $"{deckTwin.Id}-has-{nextCard.Id}");

            // add the card to the player hand
            string handToCardRel = $"{handTwin.Id}-has-{nextCard.Id}";
            await dtclient.CreateOrReplaceRelationshipAsync(handTwin.Id, handToCardRel, new BasicRelationship { TargetId = nextCard.Id, Name = "has_cards", Properties = { { "cardOrder", iorder } } });

            return (topOfDeck, Card.CreateName(((JsonElement)nextCard.Contents["Value"]).ToString(), ((JsonElement)nextCard.Contents["Suite"]).ToString()));
        }



        public static async Task<BasicDigitalTwin> GetFirstCard(DigitalTwinsClient dtclient, BasicDigitalTwin hand)
        {
            AsyncPageable<BasicRelationship> relationships = dtclient.GetRelationshipsAsync<BasicRelationship>(hand.Id);
            await foreach (BasicRelationship rel in relationships)
            {
                if (int.Parse(((JsonElement)rel.Properties["cardOrder"]).ToString()) == 0)
                {
                    return await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(rel.TargetId);
                }

            }

            return null;
        }


        public static async Task<(List<BasicDigitalTwin> playerCards, List<BasicDigitalTwin> dealerCards, List<BasicDigitalTwin> communityCards)> GetCards(DigitalTwinsClient dtclient,
                BasicDigitalTwin playerHandTwin, BasicDigitalTwin dealerHandTwin, BasicDigitalTwin communityHandTwin)
        {
            List<BasicDigitalTwin> playerCards = new List<BasicDigitalTwin>();
            if (playerHandTwin != null)
            {
                AsyncPageable<BasicRelationship> relationships = dtclient.GetRelationshipsAsync<BasicRelationship>(playerHandTwin.Id);
                await foreach (BasicRelationship rel in relationships)
                {
                    BasicDigitalTwin card = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(rel.TargetId);
                    playerCards.Add(card);
                }
            }

            List<BasicDigitalTwin> dealerCards = new List<BasicDigitalTwin>();
            if (dealerHandTwin != null)
            {
                AsyncPageable<BasicRelationship> relationships = dtclient.GetRelationshipsAsync<BasicRelationship>(dealerHandTwin.Id);
                await foreach (BasicRelationship rel in relationships)
                {
                    BasicDigitalTwin card = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(rel.TargetId);
                    dealerCards.Add(card);
                }
            }

            List<BasicDigitalTwin> communityCards = new List<BasicDigitalTwin>();
            if (communityHandTwin != null)
            {
                AsyncPageable<BasicRelationship> relationships = dtclient.GetRelationshipsAsync<BasicRelationship>(communityHandTwin.Id);
                await foreach (BasicRelationship rel in relationships)
                {
                    BasicDigitalTwin card = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(rel.TargetId);
                    communityCards.Add(card);
                }
            }
            return (playerCards, dealerCards, communityCards);
        }

        public static async Task<(BasicDigitalTwin playerHandTwin, BasicDigitalTwin dealerHandTwin, BasicDigitalTwin communityHandTwin)> GetHands(DigitalTwinsClient dtclient, string playerName)
        {
            // get player hand
            BasicDigitalTwin playerHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(playerName);
            if (playerHandTwin == null)
            {
                throw new Exception("The player hand was not found");
            }

            // get dealer hand
            BasicDigitalTwin dealerHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Dealer");
            if (dealerHandTwin == null)
            {
                throw new Exception("The dealer hand was not found");
            }

            // get community hand
            BasicDigitalTwin communityHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Community");
            if (communityHandTwin == null)
            {
                throw new Exception("The dealer hand was not found");
            }

            return (playerHandTwin, dealerHandTwin, communityHandTwin);
        }

        public static async
            Task<(BasicDigitalTwin playerHandTwin, BasicDigitalTwin dealerHandTwin, BasicDigitalTwin communityHandTwin, BasicDigitalTwin discardHandTwin, BasicDigitalTwin deckTwin)>
            GetAllHandsAndDeck(DigitalTwinsClient dtclient, string playerName)
        {
            // get player hand
            BasicDigitalTwin playerHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>(playerName);
            if (playerHandTwin == null)
            {
                throw new Exception("The player hand was not found");
            }

            // get dealer hand
            BasicDigitalTwin dealerHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Dealer");
            if (dealerHandTwin == null)
            {
                throw new Exception("The dealer hand was not found");
            }

            // get community hand
            BasicDigitalTwin communityHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Community");
            if (communityHandTwin == null)
            {
                throw new Exception("The dealer hand was not found");
            }

            // get community hand
            BasicDigitalTwin discardHandTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Discard");
            if (discardHandTwin == null)
            {
                throw new Exception("The discard hand was not found");
            }

            // get deck
            BasicDigitalTwin deckTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Deck");
            if (deckTwin == null)
            {
                throw new Exception("The deck was not found");
            }


            return (playerHandTwin, dealerHandTwin, communityHandTwin, discardHandTwin, deckTwin);
        }

        public static async Task<BasicDigitalTwin> GetGame(DigitalTwinsClient dtclient)
        {
            // get game
            BasicDigitalTwin gameTwin = await dtclient.GetDigitalTwinAsync<BasicDigitalTwin>("Game");
            if (gameTwin == null)
            {
                throw new Exception("The game was not found");
            }

            return gameTwin;
        }
    }

}