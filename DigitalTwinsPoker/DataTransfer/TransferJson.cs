using CardDataAndMath;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataTransfer
{
    public class TransferJson
    {
        public static GameStatusTransfer ConvertStringToGameStatusTransfer(string gameStatus)
        {
            return JsonConvert.DeserializeObject<GameStatusTransfer>(gameStatus);
        }

        public static GameStatusTransfer CreateHandsDisplayFromString(string gameStatus)
        {
            GameStatusTransfer gJson = JsonConvert.DeserializeObject<GameStatusTransfer>(gameStatus);
            CreateHandsDisplay(ref gJson);
            return gJson;
        }

        // creates strings:
        // [0] CommunityCards
        // [1] PlayerCards
        // [2] DealerCards
        // [3] Winning Claim and Winning Hand or Statistics (this is used by console app, only)
        // [4] Winner (Dealer, "Player", Tie) (this is only used by the bot)
        // [5] Winning Hand (could be empty) (this is only used by the bot)
        public static void CreateHandsDisplay(ref GameStatusTransfer gJson)
        {
            string[] ret = new List<string>() { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty }.ToArray();

            StringBuilder sb = new StringBuilder();
            Hand playerHand = new Hand();
            Hand dealerHand = new Hand();

            for (int cc = 0; cc < gJson.Community.Cards.Length; cc++)
            {
                string cardName = Card.CreateName(gJson.Community.Cards[cc].Value, gJson.Community.Cards[cc].Suite);
                playerHand.Add(cardName, true);
                dealerHand.Add(cardName, true);
                sb.Append(cardName);
                sb.Append(" ");
            }
            ret[0] = sb.ToString();

            sb = new StringBuilder();
            for (int cc = 0; cc < gJson.Player.Cards.Length; cc++)
            {
                string cardName = Card.CreateName(gJson.Player.Cards[cc].Value, gJson.Player.Cards[cc].Suite);
                playerHand.Add(cardName);
                sb.Append(cardName);
                sb.Append(" ");
            }
            sb.Append(SetStringFiller(sb.ToString()));
            string handDescriptorPlayer = SetHandDescriptor(gJson.Player, playerHand.Count);
            sb.Append(handDescriptorPlayer);
            ret[1] = sb.ToString();
            gJson.Player.Descriptor = handDescriptorPlayer;

            sb = new StringBuilder();
            for (int cc = 0; cc < gJson.Dealer.Cards.Length; cc++)
            {
                string cardName = Card.CreateName(gJson.Dealer.Cards[cc].Value, gJson.Dealer.Cards[cc].Suite);
                dealerHand.Add(cardName);
                sb.Append(cardName);
                sb.Append(" ");
            }
            sb.Append(SetStringFiller(sb.ToString()));
            string handDescriptorDealer = SetHandDescriptor(gJson.Dealer, playerHand.Count);
            sb.Append(handDescriptorDealer);
            ret[2] = sb.ToString();
            gJson.Dealer.Descriptor = handDescriptorDealer;

            float gameProbabilityOfWinning = float.Parse(gJson.Game.ProbabilityOfWinning);

            float playerProb = float.Parse(gJson.Player.ProbabilityOfWinning);
            float dealerProb = float.Parse(gJson.Dealer.ProbabilityOfWinning);

            ret[3] = WriteProbabilitiesConsole(gJson, gJson.Game.PotentialWinner, playerProb, dealerProb, gameProbabilityOfWinning, playerHand,
                handDescriptorPlayer, handDescriptorDealer);

            ret[4] = gJson.Game.PotentialWinner;
            if (ret[4] == "Player") ret[4] = gJson.Player.Name;

            ret[5] = WriteProbabilitiesBot(gJson, gJson.Game.PotentialWinner, playerProb, dealerProb, gameProbabilityOfWinning, 
                playerHand, handDescriptorPlayer, handDescriptorDealer);


            gJson.Display.DisplayStrings = ret;

        }

        private static string WriteProbabilitiesBot(GameStatusTransfer gJson, string potentialWinner,
            float playerProb, float dealerProb, float gameProbabilityOfWinning, Hand hand,
            string handDescriptorPlayer, string handDescriptorDealer)
        {
            StringBuilder ret = new StringBuilder();

            if (gJson.Community.Cards.Length > 0)
            {

                // you can have a tie on the current hand, but going forward one player could have an advantage
                if (potentialWinner == "Player" || (potentialWinner == "Tie" && playerProb - dealerProb > 0.001F))
                {
                    if (gJson.Community.Cards.Length >= 5)
                    {
                            ret.Append(SetHandDescriptor(gJson.Player, hand.Count, true));
                    }
                    else
                    {
                        ret.AppendLine($"Probability of you improving hand (by end): {playerProb:F2}");
                        ret.AppendLine($"Probability of you losing the lead (by end): {dealerProb:F2}");
                        ret.Append($"Probability of you winning or tying with this hand: {gameProbabilityOfWinning:F2}");
                    }
                }
                else if (potentialWinner == "Dealer" || (potentialWinner == "Tie" && dealerProb - playerProb > 0.001F))
                {
                    if (gJson.Community.Cards.Length >= 5)
                    {
                            ret.Append(SetHandDescriptor(gJson.Dealer, hand.Count, true));
                    }
                    else
                    {
                        ret.AppendLine($"Probability of me improving hand (by end): {dealerProb:F2}");
                        ret.AppendLine($"Probability of me losing the lead (by end): {playerProb:F2}");
                        ret.Append($"Probability of me winning or tying with this hand: {gameProbabilityOfWinning:F2}");
                    }
                }
                else // tie
                {
                    ret.Append(SetHandDescriptor(gJson.Player, hand.Count, true));
                }
            }
            else
            {
                ret.AppendLine($"Probability of you having the best hand at the end: {playerProb:F2}");
                ret.Append($"Probability of me having the best hand at the end: {dealerProb:F2}");
            }

            return ret.ToString();
        }


        private static string WriteProbabilitiesConsole(GameStatusTransfer gJson, string potentialWinner,
            float playerProb, float dealerProb, float gameProbabilityOfWinning, Hand hand,
            string handDescriptorPlayer, string handDescriptorDealer)
        {
            StringBuilder ret = new StringBuilder();

            if (gJson.Community.Cards.Length > 0)
            {

                // you can have a tie on the current hand, but going forward one player could have an advantage
                if (potentialWinner == "Player" || (potentialWinner == "Tie" && playerProb - dealerProb > 0.001F))
                {
                    if (gJson.Community.Cards.Length >= 5)
                    {
                        ret.AppendLine("The player has won");
                        if (handDescriptorDealer == handDescriptorPlayer)
                            ret.Append(SetHandDescriptor(gJson.Player, hand.Count, true));

                    }
                    else
                    {
                        ret.AppendLine("The player is in the lead");
                        ret.AppendLine($"Probability of player improving hand (by end): {playerProb:F2}");
                        ret.AppendLine($"Probability of player losing the lead (by end): {dealerProb:F2}");
                        ret.Append($"Probability of player winning or tying with this hand: {gameProbabilityOfWinning:F2}");
                    }
                }
                else if (potentialWinner == "Dealer" || (potentialWinner == "Tie" && dealerProb - playerProb > 0.001F))
                {
                    if (gJson.Community.Cards.Length >= 5)
                    {
                        ret.AppendLine("The Dealer has won");
                        if (handDescriptorDealer == handDescriptorPlayer)
                            ret.Append(SetHandDescriptor(gJson.Dealer, hand.Count, true));

                    }
                    else
                    {
                        ret.AppendLine("The dealer is in the lead");
                        ret.AppendLine($"Probability of dealer improving hand (by end): {dealerProb:F2}");
                        ret.AppendLine($"Probability of dealer losing the lead (by end): {playerProb:F2}");
                        ret.Append($"Probability of dealer winning or tying with this hand: {gameProbabilityOfWinning:F2}");
                    }
                }
                else // tie
                {
                    ret.AppendLine($"We have a tie");
                    ret.Append(SetHandDescriptor(gJson.Player, hand.Count, true));
                }
            }
            else
            {
                ret.AppendLine($"Probability of player having the best hand at the end: {playerProb:F2}");
                ret.Append($"Probability of dealer having the best hand at the end: {dealerProb:F2}");
            }

            return ret.ToString();
        }

        private static string SetStringFiller(string initialString)
        {
            // there is a space at the end of initialString
            if (initialString.Length == 6)
            {
                return "    ";
            }
            if (initialString.Length == 7)
            {
                return "   ";
            }
            //if (initialString.Length == 8)
            return "  ";
        }

        private static string SetHandDescriptor(PlayerHand handStatus, int handSize, bool tie = false)
        {
            StringBuilder ret = new StringBuilder(); ;
            switch (handStatus.HandType)
            {
                case "High Card":
                    if (tie == false)
                        ret.Append($"{handStatus.HighCard} High");
                    else
                    {
                        string two;
                        string three;
                        string four;
                        string five;
                        if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            two = $"an {handStatus.SecondHighCard}";
                        else
                            two = $"a {handStatus.SecondHighCard}";
                        if (handStatus.ThirdHighCard == "8" || handStatus.ThirdHighCard == "Ace")
                            three = $"an {handStatus.ThirdHighCard}";
                        else
                            three = $"a {handStatus.ThirdHighCard}";
                        if (handStatus.FourthHighCard == "8" || handStatus.FourthHighCard == "Ace")
                            four = $"an {handStatus.FourthHighCard}";
                        else
                            four = $"a {handStatus.FourthHighCard}";
                        if (handStatus.FifthHighCard == "8" || handStatus.FifthHighCard == "Ace")
                            five = $"an {handStatus.FifthHighCard}";
                        else
                            five = $"a {handStatus.FifthHighCard}";

                        ret.Append($"{handStatus.HighCard} High, followed by {two}, {three}, {four}, and {five}");
                    }
                    break;
                case "Pair":
                    if (tie == false)
                    {
                        if (handSize == 2)
                            ret.Append($"Pair of {handStatus.HighCard}'s");
                        else if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            ret.Append($"Pair of {handStatus.HighCard}'s with an {handStatus.SecondHighCard}");
                        else
                            ret.Append($"Pair of {handStatus.HighCard}'s with a {handStatus.SecondHighCard}");
                    }
                    else
                    {
                        string two;
                        string three;
                        string four;
                        if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            two = $"an {handStatus.SecondHighCard}";
                        else
                            two = $"a {handStatus.SecondHighCard}";
                        if (handStatus.ThirdHighCard == "8" || handStatus.ThirdHighCard == "Ace")
                            three = $"an {handStatus.ThirdHighCard}";
                        else
                            three = $"a {handStatus.ThirdHighCard}";
                        if (handStatus.FourthHighCard == "8" || handStatus.FourthHighCard == "Ace")
                            four = $"an {handStatus.FourthHighCard}";
                        else
                            four = $"a {handStatus.FourthHighCard}";
                        
                        ret.Append($"Pair of {handStatus.HighCard}'s with {two}, followed by {three} and {four}");
                    }
                    break;
                case "Two Pair":
                    if (handStatus.ThirdHighCard == "8" || handStatus.ThirdHighCard == "Ace")
                        ret.Append($"Two Pair {handStatus.HighCard}'s and {handStatus.SecondHighCard}'s with an {handStatus.ThirdHighCard} kicker");
                    else
                        ret.Append($"Two Pair {handStatus.HighCard}'s and {handStatus.SecondHighCard}'s with a {handStatus.ThirdHighCard} kicker");
                    break;
                case "Three of a Kind":

                    if (tie == false)
                    {
                        string two;
                        if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            two = $"an {handStatus.SecondHighCard}";
                        else
                            two = $"a {handStatus.SecondHighCard}";

                        ret.Append($"Trips {handStatus.HighCard}'s with {two} kicker");
                    }
                    else
                    {
                        string two;
                        string three;
                        if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            two = $"an {handStatus.SecondHighCard}";
                        else
                            two = $"a {handStatus.SecondHighCard}";
                        if (handStatus.ThirdHighCard == "8" || handStatus.ThirdHighCard == "Ace")
                            three = $"an {handStatus.ThirdHighCard}";
                        else
                            three = $"a {handStatus.ThirdHighCard}";

                        ret.Append($"Trips {handStatus.HighCard}'s with {two} kicker, followed by {three}");
                    }
                    break;
                case "Straight":
                    if (handStatus.HighCard == "Ace" && handStatus.SecondHighCard == "5")
                        ret.Append($"{handStatus.SecondHighCard} High Straight");
                    else
                        ret.Append($"{handStatus.HighCard} High Straight");
                    break;
                case "Flush":
                    ret.Append($"{handStatus.HighCard} High Flush");
                    break;
                case "Full House":
                    ret.Append($"Full House {handStatus.HighCard}'s over {handStatus.SecondHighCard}'s");
                    break;
                case "Four of a Kind":
                    if (tie == false)
                        ret.Append($"Quads {handStatus.HighCard}'s");
                    else
                    {
                        if (handStatus.SecondHighCard == "8" || handStatus.SecondHighCard == "Ace")
                            ret.Append($"Quads {handStatus.HighCard}'s, followed by an {handStatus.SecondHighCard}");
                        else
                            ret.Append($"Quads {handStatus.HighCard}'s, followed by a {handStatus.SecondHighCard}");
                    }
                    break;
                case "Straight Flush":
                    if (handStatus.HighCard == "Ace" && handStatus.SecondHighCard == "5")
                        ret.Append($"{handStatus.SecondHighCard} High Straight Flush");
                    else if (handStatus.HighCard == "Ace")
                        ret.Append($"Royal Flush");
                    else
                        ret.Append($"{handStatus.HighCard} High Straight Flush");
                    break;
                default:
                    ret.Append($"{handStatus.HandType} {handStatus.HighCard}");
                    break;
            }

            if (tie == false)
            {
                foreach (var p in handStatus.Potentials)
                {
                    switch (p)
                    {
                        case "ThreeOfAKind":
                            // No dealer calls out potential trips
                            break;
                        case "FullHouse":
                            if (handStatus.HandType != "Full House")
                                ret.Append(", Potential Full House");
                            break;
                        case "FourOfAKind":
                            if (handStatus.HandType != "Four Of A Kind")
                                ret.Append(", Potential Quads");
                            break;
                        case "StraightFlush":
                            if (handStatus.HandType != "Straight Flush")
                            {
                                if (handStatus.HighCard == "Ace")
                                    ret.Append(", Potential Royal Flush");
                                else
                                    ret.Append(", Potential Straight Flush");
                            }
                            break;
                        case "Flush":
                            if (!handStatus.Potentials.Any(s => s.Contains("StraightFlush")) && handStatus.HandType != "Flush")
                                ret.Append($", Potential Flush");
                            break;
                        case "Straight":
                            if (!handStatus.Potentials.Any(s => s.Contains("StraightFlush")) && handStatus.HandType != "Straight")
                                ret.Append($", Potential Straight");
                            break;
                        default:
                            if (handStatus.HandType != p)
                                ret.Append($", Potential {p}");
                            break;
                    }
                }
            }

            return ret.ToString();
        }

    }
}
