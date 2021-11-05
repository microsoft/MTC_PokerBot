using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// non calculated stats from from http://www.natesholdem.com


namespace CardDataAndMath
{
    // calculates the winner for Texas Hold'Em
    static public class CalculateWinner
    {
        static public (float winnerPercentage, int dealerWinner, float playerPercentage, float dealerPercentage) GetWinner(Hand playerHand, Hand dealerHand,
            string lastRoundWinner = "Tie", float lastRoundPlayerPercentage = 50.0F, float lastRoundDealerPercentage = 50.0F)
        {
            if (playerHand.Count != dealerHand.Count)
                throw new ArgumentException("The dealer and player hands must have the same number of cards");
            if (playerHand.Count == 0)
                throw new ArgumentException("The dealer and player hands must have > 0 cards");
            if (playerHand.Count > 7)
                throw new ArgumentException("The dealer and player hands must have <= 7 cards");
            if (playerHand.Count == 3 || playerHand.Count == 4)
                throw new ArgumentException("The dealer and player hands must have 1, 2, 5, 6, or 7 cards");

            return CalculcateCardWinner(playerHand, dealerHand, lastRoundWinner, lastRoundPlayerPercentage, lastRoundDealerPercentage);
        }

        // dealerWinner 0 == false, 1 == true, 2 == tie 
        static private (float winnerPercentage, int dealerWinner, float playerPercentage, float dealerPercentage) CalculcateCardWinner(Hand playerHand, Hand dealerHand,
            string lastRoundWinner, float lastRoundPlayerPercentage, float lastRoundDealerPercentage)
        {
            float playerPercentage;
            float dealerPercentage;
            int dealerWinner = 0;
            switch (playerHand.Count)
            {
                case 2:
                    playerPercentage = Calculate2Card(playerHand);
                    dealerPercentage = Calculate2Card(dealerHand);
                    float totalP = dealerPercentage + playerPercentage;
                    playerPercentage = (float)Math.Round(100.0F * playerPercentage / totalP, 2);
                    dealerPercentage = (float)Math.Round(100.0F * dealerPercentage / totalP, 2);

                    if (dealerPercentage > playerPercentage)
                        dealerWinner = 1;
                    else if (dealerPercentage == playerPercentage)
                        dealerWinner = 2;

                    break;
                case 5:
                case 6:
                case 7:
                    playerHand.CalculateHand(true);
                    dealerHand.CalculateHand(true);
                    (dealerWinner, playerPercentage, dealerPercentage) = CalculateWinningHandPercentages(playerHand, dealerHand);
                    break;
                default:
                    // 1 card
                    playerPercentage = Calculate1Card(playerHand);
                    dealerPercentage = Calculate1Card(dealerHand);
                    if (dealerPercentage > playerPercentage)
                        dealerWinner = 1;
                    else if (dealerPercentage == playerPercentage)
                        dealerWinner = 2;

                    break;
            }

            int lastRoundWinnerFlag = 2;
            if (lastRoundWinner == "Dealer") lastRoundWinnerFlag = 1;
            else if (lastRoundWinner == "Player") lastRoundWinnerFlag = 0;

            float winnerPercentage = CalculateWinningGamePercentage(dealerWinner, playerPercentage, dealerPercentage, lastRoundWinnerFlag, lastRoundPlayerPercentage, lastRoundDealerPercentage);

            return (winnerPercentage, dealerWinner, playerPercentage, dealerPercentage);
        }

        public static IEnumerable<T[]> CombinationsOfFive<T>(IEnumerable<T> source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            T[] data = source.ToArray();

            return Enumerable
              .Range(1, 1 << (data.Length))
              .Select(index => data
                 .Where((v, i) => (index & (1 << i)) != 0)
                 .ToArray());
        }

        static public (int dealerWinner, float playerPercentage, float dealerPercentage) CalculateWinningHandPercentages(Hand playerHand, Hand dealerHand)
        { 
            // 0 = player, 1 = dealer, 2 = tie
            int winningHand = CalculateTheWinner(Hand.TargetHands,
                playerHand.Description.handType, playerHand.Description.highCard, playerHand.Description.secondHighCard, playerHand.Description.thirdHighCard, playerHand.Description.fourthHighCard, playerHand.Description.fifthHighCard,
                dealerHand.Description.handType, dealerHand.Description.highCard, dealerHand.Description.secondHighCard, dealerHand.Description.thirdHighCard, dealerHand.Description.fourthHighCard, dealerHand.Description.fifthHighCard);

            float playerPercentage;
            float dealerPercentage;

            if (playerHand.Count == 7)
            {
                if (winningHand == 0)
                {
                    playerPercentage = 100.0F;
                    dealerPercentage = 0.0F;
                }
                else if (winningHand == 1)
                {
                    playerPercentage = 0.0F;
                    dealerPercentage = 100.0F;
                }
                else
                {
                    playerPercentage = 50.0F;
                    dealerPercentage = 50.0F;
                }
            }
            else // count == 5 or 6 only
            {
                HashSet<Card> cardsLeft = new HashSet<Card>();
                Deck deck = new Deck();
                deck.CreateNewDeck();

                List<Card> cardsinUse = new List<Card>();
                foreach (var h in playerHand)
                {
                    cardsinUse.Add(new Card(h.Name));
                }
                foreach (var h in dealerHand)
                {
                    cardsinUse.Add(new Card(h.Name));
                }

                foreach (var d in deck)
                {
                    if (cardsinUse.Contains(d)) continue;
                    cardsLeft.Add(d);
                }

                HashSet<Card> dealerOuts = null;
                HashSet<Card> playerOuts = null;
                HashSet<Card> dealerOutsFlush = null;
                HashSet<Card> playerOutsFlush = null;
                HashSet<Card> dealerOutsFullHouse = null;
                HashSet<Card> playerOutsFullHouse = null;
                List<string> dSuiteFlushes;
                List<string> pSuiteFlushes;
                int pDotFhTrip;
                int dDotFhTrip;

                if (winningHand == 0)
                {
                    (playerOuts, playerOutsFlush, playerOutsFullHouse) = GetOutsPercentage(cardsLeft, playerHand, playerHand.Description.handType, playerHand.Description.highCard, playerHand.Description.secondHighCard, 
                        out pSuiteFlushes, out pDotFhTrip);
                    (dealerOuts, dealerOutsFlush, dealerOutsFullHouse) = GetOutsPercentage(cardsLeft, dealerHand, playerHand.Description.handType, playerHand.Description.highCard, playerHand.Description.secondHighCard, 
                        out dSuiteFlushes, out dDotFhTrip);
                }
                else if (winningHand == 1)
                {
                    (playerOuts, playerOutsFlush, playerOutsFullHouse) = GetOutsPercentage(cardsLeft, playerHand, dealerHand.Description.handType, dealerHand.Description.highCard, dealerHand.Description.secondHighCard, 
                        out pSuiteFlushes, out pDotFhTrip);
                    (dealerOuts, dealerOutsFlush, dealerOutsFullHouse) = GetOutsPercentage(cardsLeft, dealerHand, dealerHand.Description.handType, dealerHand.Description.highCard, dealerHand.Description.secondHighCard, 
                        out dSuiteFlushes, out dDotFhTrip);
                }
                else
                {
                    (playerOuts, playerOutsFlush, playerOutsFullHouse) = GetOutsPercentage(cardsLeft, playerHand, playerHand.Description.handType, playerHand.Description.highCard, playerHand.Description.secondHighCard, 
                        out pSuiteFlushes, out pDotFhTrip);
                    (dealerOuts, dealerOutsFlush, dealerOutsFullHouse) = GetOutsPercentage(cardsLeft, dealerHand, dealerHand.Description.handType, dealerHand.Description.highCard, dealerHand.Description.secondHighCard, 
                        out dSuiteFlushes, out dDotFhTrip);
                }

                DuelingFlushesOutsRemoval(dealerHand, playerHand, dealerOutsFlush, playerOutsFlush, dSuiteFlushes, pSuiteFlushes);
                // since the flushes are sorted, add all the outs back together
                foreach (var c in playerOutsFlush)
                {
                    playerOuts.Add(c);
                }
                foreach (var c in dealerOutsFlush)
                {
                    dealerOuts.Add(c);
                }

                DuelingFullHouseOutsRemoval(dealerHand, playerHand, dealerOutsFullHouse, playerOutsFullHouse, dDotFhTrip, pDotFhTrip);
                foreach (var c in playerOutsFullHouse)
                {
                    playerOuts.Add(c);
                }
                foreach (var c in dealerOutsFullHouse)
                {
                    dealerOuts.Add(c);
                }

                int playerNumOuts = 0;
                int dealerNumOuts = 0;

                //if the out will help both players, we don't count this
                foreach (Card pc in playerOuts)
                {
                    if (dealerOuts.Contains(pc))
                        continue;
                    playerNumOuts++;
                }

                foreach (Card pc in dealerOuts)
                {
                    if (playerOuts.Contains(pc))
                        continue;
                    dealerNumOuts++;
                }

                if (playerHand.Count != 6)
                {
                    playerPercentage = TurnAndRiverOuts(playerNumOuts);
                    dealerPercentage = TurnAndRiverOuts(dealerNumOuts);
                }
                else
                {
                    playerPercentage = RiverOuts(playerNumOuts);
                    dealerPercentage = RiverOuts(dealerNumOuts);
                }

                // here is the case where one of the sides has no chance, however, there could be a 
                // community hand which would keep the other side from winning.  
                int boardOuts = 0;
                boardOuts = CalculateCommunityOuts(playerHand, dealerHand, winningHand, cardsLeft);
                float boardPercentage;
                if (playerHand.Count != 6)
                {
                    boardPercentage = TurnAndRiverOuts(boardOuts);
                }
                else
                {
                    boardPercentage = RiverOuts(boardOuts);
                }

                if (Math.Abs(playerPercentage - 0) < 0.001 || Math.Abs(dealerPercentage - 0) < 0.001)
                {
                    if (winningHand == 1)
                        playerPercentage += boardPercentage;
                    else
                        dealerPercentage += boardPercentage;
                }
                else
                {
                    playerPercentage += boardPercentage;
                    dealerPercentage += boardPercentage;
                }
            }

            return (winningHand, playerPercentage, dealerPercentage);
        }

        private static void DuelingFullHouseOutsRemoval(Hand dealerHand, Hand playerHand, HashSet<Card> dealerOutsFullHouse, HashSet<Card> playerOutsFullHouse, int dDotFhTrip, int pDotFhTrip)
        {
            // either we don't have tie potential or we have the actual true tie
            if (dDotFhTrip == 0 || pDotFhTrip == 0 || dDotFhTrip == pDotFhTrip)
                return;

            foreach (var c in dealerOutsFullHouse.Where(s => s.value == pDotFhTrip).ToList())
            {
                dealerOutsFullHouse.Remove(c);
            }

            foreach (var c in playerOutsFullHouse.Where(s => s.value == dDotFhTrip).ToList())
            {
                playerOutsFullHouse.Remove(c);
            }

        }

        private static void DuelingFlushesOutsRemoval(Hand dealerHand, Hand playerHand, HashSet<Card> dealerOutsFlush, HashSet<Card> playerOutsFlush, List<string> dSuiteFlushes, List<string> pSuiteFlushes)
        {
            Card[] playerFlushNumbers = new Card[15] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
            Card[] dealerFlushNumbers = new Card[15] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

            if (pSuiteFlushes.Count > 0 && dSuiteFlushes.Count > 0)
            {
                foreach (var psf in pSuiteFlushes)
                {
                    foreach (var dsf in dSuiteFlushes)
                    {
                        if (psf == dsf)
                        {
                            foreach (var c in playerHand)
                            {
                                if (c.Suite == psf) playerFlushNumbers[c.value] = c;
                            }

                            foreach (var c in dealerHand)
                            {
                                if (c.Suite == dsf) dealerFlushNumbers[c.value] = c;
                            }

                            int i;
                            for (i = 14; i >= 0; i--)
                            {
                                Card eval = playerFlushNumbers[i] ?? dealerFlushNumbers[i];
                                if (eval == null) continue;
                                if (eval.IsCommunityCard == true) continue;
                                break;
                            }

                            if (playerFlushNumbers[i] != null)
                            {
                                if (i <= 9)
                                {
                                    foreach (var c in dealerOutsFlush.Where(s => s.value < i && s.Suite == psf).ToList())
                                    {
                                        dealerOutsFlush.Remove(c);
                                    }
                                }
                                else
                                {
                                    foreach (var c in dealerOutsFlush.Where(s => s.Suite == psf).ToList())
                                    {
                                        dealerOutsFlush.Remove(c);
                                    }
                                }

                            }
                            else
                            {
                                if (i <= 9)
                                {
                                    foreach (var c in playerOutsFlush.Where(s => s.value < i && s.Suite == psf).ToList())
                                    {
                                        playerOutsFlush.Remove(c);
                                    }
                                }
                                else
                                {
                                    foreach (var c in playerOutsFlush.Where(s => s.Suite == psf).ToList())
                                    {
                                        playerOutsFlush.Remove(c);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static int CalculateCommunityOuts(Hand playerHand, Hand dealerHand, int winningHand, HashSet<Card> cardsLeft)
        {
            int boardOuts = 0;
            (int handType, int highCard, int secondHighCard, int thirdHighCard, int fourthHighCard, int fifthHighCard) description;

            if (winningHand == 0)
                description = playerHand.Description;
            else
                description = dealerHand.Description; // there should be no ties in this case

            bool dowe;
            int val;
            int val2;
            int val3;
            // comm only, so either hand
            (dowe, val, val2) = playerHand.DoWeHaveAPotentialCommunityStraight();
            if (dowe == true && description.handType < Hand.TargetHands["Straight"])
            {
                for (int i = 2; i < 14; i++)
                {
                    if (i != val && i != val2) continue;
                    for (int s = 0; s < 4; ++s)
                    {
                        if (cardsLeft.Contains(new Card($"{Card.CardValues[i]}{Card.CardSuites[s][0]}")))
                            boardOuts++;
                    }
                    (int tok, _, int f, int fh, int fok, int sf) = playerHand.Potentials;
                    playerHand.Potentials = (tok, Hand.TargetHands["Straight"], f, fh, fok, sf);
                    (tok, _, f, fh, fok, sf) = dealerHand.Potentials;
                    dealerHand.Potentials = (tok, Hand.TargetHands["Straight"], f, fh, fok, sf);

                }
            }
            else
            {
                (dowe, val) = playerHand.DoWeHaveAPotentialCommunityFlush();
                if (dowe == true && description.handType < Hand.TargetHands["Flush"])
                {
                    for (int i = 2; i < 14; i++)
                    {
                        if (cardsLeft.Contains(new Card($"{Card.CardValues[i]}{Card.CardSuites[val][0]}")))
                            boardOuts++;
                    }
                    (int tok, int st, _, int fh, int fok, int sf) = playerHand.Potentials;
                    playerHand.Potentials = (tok, st, Hand.TargetHands["Flush"], fh, fok, sf);
                    (tok, st, _, fh, fok, sf) = dealerHand.Potentials;
                    dealerHand.Potentials = (tok, st, Hand.TargetHands["Flush"], fh, fok, sf);
                }
                else
                {
                    (dowe, val, val2, val3) = playerHand.DoWeHaveAPotentialCommunityFullHouse();
                    if (dowe == true && description.handType < Hand.TargetHands["FullHouse"])
                    {
                        if (val != 0)
                        {
                            for (int s = 0; s < 4; ++s)
                            {
                                if (cardsLeft.Contains(new Card($"{Card.CardValues[val]}{Card.CardSuites[s][0]}")))
                                    boardOuts++;
                            }
                            (int tok, int st, int f, _, int fok, int sf) = playerHand.Potentials;
                            playerHand.Potentials = (tok, st, f, Hand.TargetHands["FullHouse"], fok, sf);
                            (tok, st, f, _, fok, sf) = dealerHand.Potentials;
                            dealerHand.Potentials = (tok, st, f, Hand.TargetHands["FullHouse"], fok, sf);

                        }
                        else
                        {
                            for (int i = 2; i < 14; i++)
                            {
                                if (i != val2 && i != val3) continue;
                                for (int s = 0; s < 4; ++s)
                                {
                                    if (cardsLeft.Contains(new Card($"{Card.CardValues[i]}{Card.CardSuites[s][0]}")))
                                        boardOuts++;
                                }
                            }
                            (int tok, int st, int f, _, int fok, int sf) = playerHand.Potentials;
                            playerHand.Potentials = (tok, st, f, Hand.TargetHands["FullHouse"], fok, sf);
                            (tok, st, f, _, fok, sf) = dealerHand.Potentials;
                            dealerHand.Potentials = (tok, st, f, Hand.TargetHands["FullHouse"], fok, sf);
                        }
                    }
                    else
                    {
                        (dowe, val) = playerHand.DoWeHaveACommunityThreeOfAKind(); // potential quads
                        if (dowe == true && description.handType < Hand.TargetHands["FourOfAKind"])
                        {
                            for (int i = 2; i < 14; i++)
                            {
                                if (i != val) continue;
                                for (int s = 0; s < 4; ++s)
                                {
                                    if (cardsLeft.Contains(new Card($"{Card.CardValues[i]}{Card.CardSuites[s][0]}")))
                                        boardOuts++;
                                }
                            }
                            (int tok, int st, int f, int fh, _, int sf) = playerHand.Potentials;
                            playerHand.Potentials = (tok, st, f, fh, Hand.TargetHands["FourOfAKind"], sf);
                            (tok, st, f, fh, _, sf) = dealerHand.Potentials;
                            dealerHand.Potentials = (tok, st, f, fh, Hand.TargetHands["FourOfAKind"], sf);
                        }
                        else
                        {
                            (dowe, val, val2, val3) = playerHand.DoWeHaveAPotentialCommunityStraightFlush(); 
                            if (dowe == true && description.handType < Hand.TargetHands["StraightFlush"])
                            {
                                for (int i = 2; i < 14; i++)
                                {
                                    if (i != val2 && i != val3) continue;
                                    if (cardsLeft.Contains(new Card($"{Card.CardValues[i]}{Card.CardSuites[val][0]}")))
                                        boardOuts++;
                                }
                                (int tok, int st, int f, int fh, int fok, _) = playerHand.Potentials;
                                playerHand.Potentials = (tok, st, f, fh, fok, Hand.TargetHands["StraightFlush"]);
                                (tok, st, f, fh, fok, _) = dealerHand.Potentials;
                                dealerHand.Potentials = (tok, st, f, fh, fok, Hand.TargetHands["StraightFlush"]);
                            }
                        }
                    }
                }

            }

            return boardOuts;
        }

        // this will only work if hand.Count == 5 || hand.Count == 6
        private static (HashSet<Card>, HashSet<Card>, HashSet<Card>) GetOutsPercentage(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, int secondHighCard, 
            out List<string> suiteFlushes, out int potFhTrip)
        {
            HashSet<Card> hashOuts = new HashSet<Card>();
            HashSet<Card> hashOutsFlush = new HashSet<Card>();
            HashSet<Card> hashOutsFullHouse = new HashSet<Card>();
            int potentialStraight = 0;
            int potentialFlush = 0;
            int potentialStraightFlush = 0;
            int potentialFourOfAKind = 0;
            int potentialFullHouse = 0;
            int potentialThreeOfAKind = 0;

            // you don't calculate high card outs
            hashOuts = CalculatePairOuts(cardsLeft, hand, handType, highCard, hashOuts);
            hashOuts = CalculateTwoPairOuts(cardsLeft, hand, handType, highCard, secondHighCard, hashOuts);
            (hashOuts, potentialThreeOfAKind) = CalculateThreeOfAKindOuts(cardsLeft, hand, handType, highCard, hashOuts, potentialThreeOfAKind);
            (hashOuts, potentialStraight) = CalculateStraightOuts(false, cardsLeft, hand, handType, highCard, hashOuts, potentialStraight);
            (hashOutsFlush, potentialFlush, suiteFlushes) = CalculateFlushOuts(cardsLeft, hand, handType, highCard, hashOutsFlush, potentialFlush);
            (hashOutsFullHouse, potentialFullHouse, potFhTrip) = CalculateFullHouseOuts(cardsLeft, hand, handType, highCard, hashOuts, potentialFullHouse);
            (hashOuts, potentialFourOfAKind) = CalculateFourOfAKindOuts(cardsLeft, hand, handType, highCard, hashOuts, potentialFourOfAKind);
            (hashOuts, potentialStraightFlush) = CalculateStraightOuts(true, cardsLeft, hand, handType, highCard, hashOuts, potentialStraightFlush);

            hand.Potentials = (potentialThreeOfAKind, potentialStraight, potentialFlush, potentialFullHouse, potentialFourOfAKind, potentialStraightFlush);

            return (hashOuts, hashOutsFlush, hashOutsFullHouse);
        }

        private static (HashSet<Card>, int) CalculateFourOfAKindOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts, int potentialFourOfAKind)
        {
            // we must have at lease one pair or trips since we only get one card
            if (((hand.Count == 6 && (hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] || hand.Description.handType == Hand.TargetHands["FullHouse"]))
                || (hand.Count == 5 && (hand.Description.handType == Hand.TargetHands["TwoPair"] || hand.Description.handType == Hand.TargetHands["Pair"]
                || hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] || hand.Description.handType == Hand.TargetHands["FullHouse"])))
                && handType <= Hand.TargetHands["FourOfAKind"])
            {

                int starting = 0;
                if (handType == Hand.TargetHands["FourOfAKind"]) starting = highCard + 1;

                for (int c = starting; c < 15; ++c)
                {
                    // we need at least 1 card, since we only get two more, but we don't want more for trips
                    bool noCommCards = false;
                    for (int h = 0; h < hand.Count; h++)
                    {
                        if (hand[h].value == c)
                        {
                            if ((hand.Count == 5 && ((hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] && hand[h].value != hand.Description.highCard)
                                || (hand.Description.handType == Hand.TargetHands["FullHouse"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard)
                                || (hand.Description.handType == Hand.TargetHands["TwoPair"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard)
                                || (hand.Description.handType == Hand.TargetHands["Pair"] && hand[h].value != hand.Description.highCard)))
                                || (hand.Count == 6 && ((hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] && hand[h].value != hand.Description.highCard)
                                || (hand.Description.handType == Hand.TargetHands["FullHouse"] && hand[h].value != hand.Description.highCard))))
                                continue;

                            if (hand[h].IsCommunityCard == false)
                                noCommCards = true;
                        }

                        if (hand[h].value == c && noCommCards == true)
                        {
                            if ((hand.Count == 5 && ((hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] && hand[h].value != hand.Description.highCard)
                                || (hand.Description.handType == Hand.TargetHands["FullHouse"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard)
                                || (hand.Description.handType == Hand.TargetHands["TwoPair"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard)
                                || (hand.Description.handType == Hand.TargetHands["Pair"] && hand[h].value != hand.Description.highCard)))
                                || (hand.Count == 6 && ((hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] && hand[h].value != hand.Description.highCard)
                                || (hand.Description.handType == Hand.TargetHands["FullHouse"] && hand[h].value != hand.Description.highCard))))
                                continue;

                            for (int s = 0; s < 4; ++s)
                            {
                                Card crd = new Card($"{Card.CardValues[c]}{Card.CardSuites[s][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                    if (hand.Description.handType == Hand.TargetHands["ThreeOfAKind"])
                                        potentialFourOfAKind = Hand.TargetHands["FourOfAKind"];
                                }
                            }
                        }
                    }
                }
            }

            return (hashOuts, potentialFourOfAKind);
        }

        private static (HashSet<Card>, int, int) CalculateFullHouseOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts, int potentialFullHouse)
        {
            int potFhTrip = 0;
            // if we have a community pair, and we are a one pair hand, then we don't have a potential full house, as the community pair is the only pair
            if ((hand.Description.handType == Hand.TargetHands["TwoPair"] 
                || hand.Description.handType == Hand.TargetHands["ThreeOfAKind"]) 
                && handType <= Hand.TargetHands["FullHouse"])
            {

                int starting = 0;
                if (handType == Hand.TargetHands["FullHouse"]) starting = highCard + 1;

                for (int c = starting; c < 15; ++c)
                {
                    for (int h = 0; h < hand.Count; h++)
                    {
                        if (hand[h].value == c)
                        {
                            if ((hand.Description.handType == Hand.TargetHands["ThreeOfAKind"] && hand[h].value == hand.Description.highCard)
                            || (hand.Count == 6 && hand.Description.handType == Hand.TargetHands["TwoPair"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard))
                                continue;

                            for (int s = 0; s < 4; ++s)
                            {
                                Card crd = new Card($"{Card.CardValues[c]}{Card.CardSuites[s][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                    potentialFullHouse = Hand.TargetHands["FullHouse"];
                                    potFhTrip = hand.Description.highCard;
                                }
                            }
                        }
                    }
                }
            }
            return (hashOuts, potentialFullHouse, potFhTrip);

        }

        private static (HashSet<Card>, int, List<string> suiteFlushes) CalculateFlushOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts, int potentialFlush)
        {
            string spadeFlush = string.Empty;
            string heartFlush = string.Empty;
            string clubFlush = string.Empty;
            string diamondFlush = string.Empty;

            if (handType <= Hand.TargetHands["Flush"])
            {
                int spades = 0;
                int clubs = 0;
                int hearts = 0;
                int diamonds = 0;
                bool spadesNonCom = false;
                bool clubsNonCom = false;
                bool heartsNonCom = false;
                bool diamondsNonCom = false;
                // for flush vs flush
                int highSpades = 0;
                int highClubs = 0;
                int highHearts = 0;
                int highDiamonds = 0;

                // we just cannot beat an Ace High, else anything is possible
                if (handType == Hand.TargetHands["Flush"] && highCard == 14) return (hashOuts, potentialFlush, new List<string>());

                foreach (var h in hand)
                {
                    switch (h.Suite)
                    {
                        case "Spade":
                            spades++;
                            if (h.IsCommunityCard == false)
                                spadesNonCom = true;
                            highSpades = h.value;
                            break;
                        case "Heart":
                            hearts++;
                            if (h.IsCommunityCard == false)
                                heartsNonCom = true;
                            highHearts = h.value;
                            break;
                        case "Club":
                            clubs++;
                            if (h.IsCommunityCard == false)
                                clubsNonCom = true;
                            highClubs = h.value;
                            break;
                        case "Diamond":
                            diamonds++;
                            if (h.IsCommunityCard == false)
                                diamondsNonCom = true;
                            highDiamonds = h.value;
                            break;
                    }
                }

                if (spadesNonCom == false) spades = 0;
                if (heartsNonCom == false) hearts = 0;
                if (clubsNonCom == false) clubs = 0;
                if (diamondsNonCom == false) diamonds = 0;

                string fv;
                // don't worry about removing any that we already have, they are in the hashset, 
                // so there will be no duplicates
                for (int st = 2; st < 15; st++)
                {
                    fv = string.Empty;
                    (hashOuts, potentialFlush, fv) = DetFlushHashOuts(cardsLeft, hand, handType, st, spades, highSpades,
                        "Spade", highCard, hashOuts, potentialFlush);
                    if (!string.IsNullOrWhiteSpace(fv)) spadeFlush = fv;
                    fv = string.Empty;
                    (hashOuts, potentialFlush, fv) = DetFlushHashOuts(cardsLeft, hand, handType, st, hearts, highHearts,
                        "Heart", highCard, hashOuts, potentialFlush);
                    if (!string.IsNullOrWhiteSpace(fv)) heartFlush = fv;
                    fv = string.Empty;
                    (hashOuts, potentialFlush, fv) = DetFlushHashOuts(cardsLeft, hand, handType, st, clubs, highClubs,
                        "Club", highCard, hashOuts, potentialFlush);
                    if (!string.IsNullOrWhiteSpace(fv)) clubFlush = fv;
                    fv = string.Empty;
                    (hashOuts, potentialFlush, fv) = DetFlushHashOuts(cardsLeft, hand, handType, st, diamonds, highDiamonds,
                        "Diamond", highCard, hashOuts, potentialFlush);
                    if (!string.IsNullOrWhiteSpace(fv)) diamondFlush = fv;
                }
            }

            List<string> suiteFlushes = new List<string>();
            if (!string.IsNullOrWhiteSpace(spadeFlush)) suiteFlushes.Add(spadeFlush);
            if (!string.IsNullOrWhiteSpace(heartFlush)) suiteFlushes.Add(heartFlush);
            if (!string.IsNullOrWhiteSpace(clubFlush)) suiteFlushes.Add(clubFlush);
            if (!string.IsNullOrWhiteSpace(diamondFlush)) suiteFlushes.Add(diamondFlush);

            return (hashOuts, potentialFlush, suiteFlushes);
        }

        private static (HashSet<Card>, int, string) DetFlushHashOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int st, int numOfSuite, int highNumOfSuite, 
            string Suite, int highCard, HashSet<Card> hashOuts, int potentialFlush)
        {
            if ((numOfSuite == 5 && st <= highNumOfSuite) || (numOfSuite == 4 && st <= highCard && handType == Hand.TargetHands["Flush"]) || numOfSuite < 3) 
                return (hashOuts, potentialFlush, string.Empty);

            string suiteOut = String.Empty;

            if ((hand.Count == 5 && numOfSuite >= 3) || (hand.Count == 6 && numOfSuite >= 4))
            {
                Card crd = new Card($"{Card.CardValues[st]}{Suite[0]}");
                if (cardsLeft.Contains(crd))
                {
                    hashOuts.Add(crd);
                    potentialFlush = Hand.TargetHands["Flush"];
                    suiteOut = Suite;
                }
            }
            return (hashOuts, potentialFlush, suiteOut);
        }

        // if straightFlush == true, then we only look for a straightFlush
        private static (HashSet<Card>, int) CalculateStraightOuts(bool straightFlush, HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts, 
            int potentialStraight)
        {
            if((straightFlush == false && handType <= Hand.TargetHands["Straight"])
                || (straightFlush == true && handType <= Hand.TargetHands["StraightFlush"]))
            { 
                int starting = 5;
                if ((straightFlush == false && handType == Hand.TargetHands["Straight"])
                    || (straightFlush == true && handType == Hand.TargetHands["StraightFlush"])) starting = highCard + 1;

                List<int> straightValues;
                int numFound;
                int sEnd = 1;
                if (straightFlush == true) sEnd = 4;

                for (int s = 0; s < sEnd; ++s)
                {
                    for (int c = starting; c < 15; ++c)
                    {
                        straightValues = new List<int>();
                        for (int st = c; st > c - 5; st--)
                        {
                            if (st == 1) straightValues.Add(14);
                            else straightValues.Add(st);
                        }
                        numFound = 5;

                        bool foundallInCommHand = true;
                        for (int h = 0; h < hand.Count; h++)
                        {
                            for (int jj = straightValues.Count - 1; jj >= 0; jj--)
                            {
                                if ((hand[h].value == straightValues[jj] && straightFlush == false)
                                    || (hand[h].value == straightValues[jj] && hand[h].Suite == Card.CardSuites[s] && straightFlush == true))
                                {
                                    if (hand[h].IsCommunityCard == false)
                                        foundallInCommHand = false;
                                    numFound--;
                                    straightValues.RemoveAt(jj);
                                    break;
                                }
                            }
                        }

                        if ((hand.Count == 5 && numFound > 2)
                            || (hand.Count == 6 && numFound > 1)
                            || foundallInCommHand == true)
                            continue;

                        // don't worry about removing any that we already have, they are in the hashset, 
                        // so there will be no duplicates
                        int ssEnd = 4;
                        if (straightFlush == true) ssEnd = 1;

                        for (int st = 0; st < straightValues.Count; st++)
                        {
                            for (int ss = 0; ss < ssEnd; ++ss)
                            {
                                Card crd = new Card($"{Card.CardValues[straightValues[st]]}{Card.CardSuites[ss][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                    if (straightFlush == false)
                                        potentialStraight = Hand.TargetHands["Straight"];
                                    else
                                        potentialStraight = Hand.TargetHands["StraightFlush"];

                                }
                            }
                        }
                    }
                }
            }

            return (hashOuts, potentialStraight);
        }

        private static (HashSet<Card>, int) CalculateThreeOfAKindOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts, int potentialThreeOfAKind)
        {
            // we must have at least one pair since we only get one card
            if (((hand.Count == 6 && (hand.Description.handType == Hand.TargetHands["TwoPair"] || hand.Description.handType == Hand.TargetHands["Pair"]))
                || hand.Count == 5) && handType <= Hand.TargetHands["ThreeOfAKind"])
            {

                int starting = 0;
                if (handType == Hand.TargetHands["ThreeOfAKind"]) starting = highCard + 1;

                for (int c = starting; c < 15; ++c)
                {
                    // we need at least 1 card, since we only get two more
                    for (int h = 0; h < hand.Count; h++)
                    {
                        if (hand[h].value == c && hand[h].IsCommunityCard == false)
                        {
                            if (hand.Count == 6)
                            {
                                if ((hand.Description.handType == Hand.TargetHands["Pair"] && hand[h].value != hand.Description.highCard)
                                    || (hand.Description.handType == Hand.TargetHands["TwoPair"] && hand[h].value != hand.Description.highCard && hand[h].value != hand.Description.secondHighCard))
                                    continue;
                            }

                            for (int s = 0; s < 4; ++s)
                            {
                                Card crd = new Card($"{Card.CardValues[c]}{Card.CardSuites[s][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                    if (hand.Description.handType == Hand.TargetHands["Pair"] || hand.Description.handType == Hand.TargetHands["TwoPair"])
                                        potentialThreeOfAKind = Hand.TargetHands["ThreeOfAKind"];
                                }
                            }
                        }
                    }
                }
            }

            return (hashOuts, potentialThreeOfAKind);
        }

        private static HashSet<Card> CalculateTwoPairOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, int secondHighCard, HashSet<Card> hashOuts)
        { 
            bool dowe;
            int cpv;
            (dowe, cpv) = hand.DoWeHaveACommunityPair();
            if ((hand.Description.handType == Hand.TargetHands["Pair"] || dowe == true) && handType <= Hand.TargetHands["TwoPair"])
            {
                // get the pair that we have
                int hv;
                (_, hv, _, _, _, _) = hand.Description;
                if (cpv > hv) hv = cpv;

                int starting = 0;
                if (handType == Hand.TargetHands["TwoPair"])
                {
                    // if as good as the high card, we want the best possible second only
                    if (hv >= highCard)
                        starting = secondHighCard + 1;
                    // else we need to only look at the highest
                    else
                        starting = highCard + 1;
                }

                List<Card> foundCards = new List<Card>();
                for (int c = starting; c< 15; ++c)
                {
                    if (hand.Description.highCard == c)
                        continue;

                    for (int h = 0; h<hand.Count; h++)
                    {
                        if (hand[h].value == c && hand[h].IsCommunityCard == false)
                        {
                            for (int s = 0; s< 4; ++s)
                            {
                                Card crd = new Card($"{Card.CardValues[c]}{Card.CardSuites[s][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                }
                            }
                        }
                    }
                }
            }
            return hashOuts;
        }


        // for the outs functions the handtype and highcard, etc may be different than what is in the hand,
        // so the outs are restricted to beat the additional info (it's what the other player has)
        private static HashSet<Card> CalculatePairOuts(HashSet<Card> cardsLeft, Hand hand, int handType, int highCard, HashSet<Card> hashOuts)
        {
            if (hand.Description.handType == Hand.TargetHands["HighCard"] && handType <= Hand.TargetHands["Pair"])
            {
                int starting = 0;
                if (handType == Hand.TargetHands["Pair"])
                {
                    starting = highCard + 1;
                }

                for (int c = starting; c < 15; ++c)
                {
                    for (int h = 0; h < hand.Count; h++)
                    {
                        if (hand[h].value == c && hand[h].IsCommunityCard == false)
                        {
                            for (int s = 0; s < 4; ++s)
                            {
                                Card crd = new Card($"{Card.CardValues[c]}{Card.CardSuites[s][0]}");
                                if (cardsLeft.Contains(crd))
                                {
                                    hashOuts.Add(crd);
                                }
                            }
                        }
                    }
                }
            }

            return hashOuts;
        }

        private static int CalculateTheWinner(Dictionary<string, int> TargetHands, int playerHandType, int playerHighCard, int playerSecondHighCard, int playerThirdHighCard, int playerFourthHighCard, int playerFifthHighCard, 
            int dealerHandType, int dealerHighCard, int dealerSecondHighCard, int dealerThirdHighCard, int dealerFourthHighCard, int dealerFifthHighCard)
        {
            int winningHand;
            if (playerHandType > dealerHandType) winningHand = 0;
            else if (dealerHandType > playerHandType) winningHand = 1;
            else
            {
                if (playerHandType == TargetHands["StraightFlush"]
                    || playerHandType == TargetHands["FourOfAKind"]
                    || playerHandType == TargetHands["Flush"]
                    || playerHandType == TargetHands["Straight"]
                    )
                {
                    if (playerHighCard > dealerHighCard) winningHand = 0;
                    else if (dealerHighCard > playerHighCard) winningHand = 1;
                    else winningHand = 2;
                }
                else if (playerHandType == TargetHands["FullHouse"])
                {
                    if (playerHighCard > dealerHighCard) winningHand = 0;
                    else if (dealerHighCard > playerHighCard) winningHand = 1;
                    else if (playerSecondHighCard > dealerSecondHighCard) winningHand = 0;
                    else if (dealerSecondHighCard > playerSecondHighCard) winningHand = 1;
                    else winningHand = 2;
                }
                else if (playerHandType == TargetHands["ThreeOfAKind"]
                          || playerHandType == TargetHands["TwoPair"])
                {
                    if (playerHighCard > dealerHighCard) winningHand = 0;
                    else if (dealerHighCard > playerHighCard) winningHand = 1;
                    else if (playerSecondHighCard > dealerSecondHighCard) winningHand = 0;
                    else if (dealerSecondHighCard > playerSecondHighCard) winningHand = 1;
                    else if (playerThirdHighCard > dealerThirdHighCard) winningHand = 0;
                    else if (dealerThirdHighCard > playerThirdHighCard) winningHand = 1;
                    else winningHand = 2;
                }
                else if (playerHandType == TargetHands["Pair"])
                {
                    if (playerHighCard > dealerHighCard) winningHand = 0;
                    else if (dealerHighCard > playerHighCard) winningHand = 1;
                    else if (playerSecondHighCard > dealerSecondHighCard) winningHand = 0;
                    else if (dealerSecondHighCard > playerSecondHighCard) winningHand = 1;
                    else if (playerThirdHighCard > dealerThirdHighCard) winningHand = 0;
                    else if (dealerThirdHighCard > playerThirdHighCard) winningHand = 1;
                    else if (playerFourthHighCard > dealerFourthHighCard) winningHand = 0;
                    else if (dealerFourthHighCard > playerFourthHighCard) winningHand = 1;
                    else winningHand = 2;
                }
                else //if (playerHandType == playerHand.TargetHands["HighCard"])
                {
                    if (playerHighCard > dealerHighCard) winningHand = 0;
                    else if (dealerHighCard > playerHighCard) winningHand = 1;
                    else if (playerSecondHighCard > dealerSecondHighCard) winningHand = 0;
                    else if (dealerSecondHighCard > playerSecondHighCard) winningHand = 1;
                    else if (playerThirdHighCard > dealerThirdHighCard) winningHand = 0;
                    else if (dealerThirdHighCard > playerThirdHighCard) winningHand = 1;
                    else if (playerFourthHighCard > dealerFourthHighCard) winningHand = 0;
                    else if (dealerFourthHighCard > playerFourthHighCard) winningHand = 1;
                    else if (playerFifthHighCard > dealerFifthHighCard) winningHand = 0;
                    else if (dealerFifthHighCard > playerFifthHighCard) winningHand = 1;
                    else winningHand = 2;
                }
            }

            return winningHand;
        }

        static private float TurnAndRiverOuts(int outs)
        {
            switch(outs)
            {
                case 1:
                    return 4.26F;
                case 2:
                    return 8.42F;
                case 3:
                    return 12.49F;
                case 4:
                    return 16.47F;
                case 5:
                    return 20.35F;
                case 6:
                    return 24.14F;
                case 7:
                    return 27.85F;
                case 8:
                    return 31.45F;
                case 9:
                    return 34.97F;
                case 10:
                    return 38.39F;
                case 11:
                    return 41.72F;
                case 12:
                    return 44.96F;
                case 13:
                    return 48.1F;
                case 14:
                    return 51.16F;
                case 15:
                    return 54.12F;
                case 16:
                    return 56.98F;
                case 17:
                    return 59.76F;
                case 18:
                    return 62.44F;
                case 19:
                    return 65.03F;
                case 20:
                    return 67.53F;
                case 21:
                    return 69.94F;
                default:
                    return (outs * 4) - (outs - 8);
            }
        }

        static private float RiverOuts(int outs)
        {
            return (float)Math.Round(outs / 46.0F * 100.0F, 1);
        }


        static public float Calculate2Card(Hand hand)
        {
            if (hand[0].value == hand[1].value)
            {
                hand.Description = (2, hand[1].value, 0, 0, 0, 0);
            }
            else
            {
                hand.Description = (1, hand[1].value, hand[0].value, 0, 0, 0);
            }

            switch (hand[1].Value)
            {
                case "Ace":
                    switch(hand[0].Value)
                    {
                        case "Ace":
                            return 85.3F;
                        case "King":
                            if (hand[1].Suite == hand[0].Suite)
                                return 67.0F;
                            else return 65.4F;
                        case "Queen":
                            if (hand[1].Suite == hand[0].Suite)
                                return 66.1F;
                            else return 64.5F;
                        case "Jack":
                            if (hand[1].Suite == hand[0].Suite)
                                return 65.4F;
                            else return 63.6F;
                        case "10":
                            if (hand[1].Suite == hand[0].Suite)
                                return 64.7F;
                            else return 62.9F;
                        case "9":
                            if (hand[1].Suite == hand[0].Suite)
                                return 63.0F;
                            else return 60.9F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 62.1F;
                            else return 60.1F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 61.1F;
                            else return 59.1F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 60.0F;
                            else return 57.8F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 59.9F;
                            else return 57.7F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 58.9F;
                            else return 56.4F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 58.0F;
                            else return 55.6F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 57.0F;
                            else return 64.6F;
                    }
                    break;
                case "King":
                    switch (hand[0].Value)
                    {
                        case "King":
                            return 82.4F;
                        case "Queen":
                            if (hand[1].Suite == hand[0].Suite)
                                return 63.4F;
                            else return 61.4F;
                        case "Jack":
                            if (hand[1].Suite == hand[0].Suite)
                                return 62.6F;
                            else return 60.6F;
                        case "10":
                            if (hand[1].Suite == hand[0].Suite)
                                return 61.9F;
                            else return 59.9F;
                        case "9":
                            if (hand[1].Suite == hand[0].Suite)
                                return 60.0F;
                            else return 58.0F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 58.5F;
                            else return 56.3F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 57.8F;
                            else return 55.4F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 56.8F;
                            else return 54.3F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 55.8F;
                            else return 53.3F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 54.7F;
                            else return 52.1F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 53.8F;
                            else return 51.2F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 52.9F;
                            else return 50.2F;
                    }
                    break;
                case "Queen":
                    switch (hand[0].Value)
                    {
                        case "Queen":
                            return 79.9F;
                        case "Jack":
                            if (hand[1].Suite == hand[0].Suite)
                                return 60.3F;
                            else return 58.2F;
                        case "10":
                            if (hand[1].Suite == hand[0].Suite)
                                return 59.5F;
                            else return 57.4F;
                        case "9":
                            if (hand[1].Suite == hand[0].Suite)
                                return 57.9F;
                            else return 55.5F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 56.2F;
                            else return 53.8F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 54.5F;
                            else return 51.9F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 53.8F;
                            else return 51.1F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 52.9F;
                            else return 50.2F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 51.7F;
                            else return 49.0F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 50.7F;
                            else return 47.9F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 49.9F;
                            else return 47.0F;
                    }
                    break;
                case "Jack":
                    switch (hand[0].Value)
                    {
                        case "Jack":
                            return 77.5F;
                        case "10":
                            if (hand[1].Suite == hand[0].Suite)
                                return 57.5F;
                            else return 55.4F;
                        case "9":
                            if (hand[1].Suite == hand[0].Suite)
                                return 55.8F;
                            else return 53.4F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 54.2F;
                            else return 51.7F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 52.4F;
                            else return 49.9F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 50.8F;
                            else return 47.9F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 50.0F;
                            else return 47.1F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 49.0F;
                            else return 46.1F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 47.9F;
                            else return 45.0F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 47.1F;
                            else return 44.0F;
                    }
                    break;
                case "10":
                    switch (hand[0].Value)
                    {
                        case "10":
                            return 75.1F;
                        case "9":
                            if (hand[1].Suite == hand[0].Suite)
                                return 54.3F;
                            else return 51.7F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 52.6F;
                            else return 50.0F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 51.0F;
                            else return 48.2F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 49.2F;
                            else return 46.3F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 47.2F;
                            else return 44.2F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 46.4F;
                            else return 43.4F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 45.5F;
                            else return 42.4F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 44.7F;
                            else return 41.5F;
                    }
                    break;
                case "9":
                    switch (hand[0].Value)
                    {
                        case "9":
                            return 72.1F;
                        case "8":
                            if (hand[1].Suite == hand[0].Suite)
                                return 51.1F;
                            else return 48.4F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 49.5F;
                            else return 46.7F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 47.7F;
                            else return 44.9F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 45.9F;
                            else return 42.9F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 43.8F;
                            else return 40.7F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 43.2F;
                            else return 39.9F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 42.3F;
                            else return 38.3F;
                    }
                    break;
                case "8":
                    switch (hand[0].Value)
                    {
                        case "8":
                            return 69.1F;
                        case "7":
                            if (hand[1].Suite == hand[0].Suite)
                                return 48.2F;
                            else return 45.5F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 46.5F;
                            else return 43.6F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 44.8F;
                            else return 41.7F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 42.7F;
                            else return 39.6F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 40.8F;
                            else return 37.5F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 40.3F;
                            else return 36.8F;
                    }
                    break;
                case "7":
                    switch (hand[0].Value)
                    {
                        case "7":
                            return 66.2F;
                        case "6":
                            if (hand[1].Suite == hand[0].Suite)
                                return 45.7F;
                            else return 42.7F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 43.8F;
                            else return 40.8F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 41.8F;
                            else return 38.6F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 40.0F;
                            else return 36.6F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 38.1F;
                            else return 34.6F;
                    }
                    break;
                case "6":
                    switch (hand[0].Value)
                    {
                        case "6":
                            return 63.3F;
                        case "5":
                            if (hand[1].Suite == hand[0].Suite)
                                return 43.2F;
                            else return 40.1F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 41.4F;
                            else return 38.0F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 39.4F;
                            else return 35.9F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 37.5F;
                            else return 34.0F;
                    }
                    break;
                case "5":
                    switch (hand[0].Value)
                    {
                        case "5":
                            return 60.3F;
                        case "4":
                            if (hand[1].Suite == hand[0].Suite)
                                return 41.1F;
                            else return 37.9F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 39.3F;
                            else return 35.8F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 37.5F;
                            else return 33.9F;
                    }
                    break;
                case "4":
                    switch (hand[0].Value)
                    {
                        case "4":
                            return 57.0F;
                        case "3":
                            if (hand[1].Suite == hand[0].Suite)
                                return 38.0F;
                            else return 34.4F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 36.3F;
                            else return 32.5F;
                    }
                    break;
                case "3":
                    switch (hand[0].Value)
                    {
                        case "3":
                            return 53.7F;
                        case "2":
                            if (hand[1].Suite == hand[0].Suite)
                                return 35.1F;
                            else return 31.2F;
                    }
                    break;
            }

            return 50.3F; //22, all that's left

        }

        static public float Calculate1Card(Hand hand)
        {
            hand.Description = (1, hand[0].value, 0, 0, 0, 0);
            return (float)Math.Round(((float)Math.Round((hand[0].value - 2F) * 4F / 52.0F, 3) * 100), 3);
        }

        static private float CalculateWinningGamePercentage(int dealerWinner, float playerPercentage, float dealerPercentage, 
            int lastRoundDealerWinner, float lastRoundPlayerPercentage, float lastRoundDealerPercentage)
        {
            float winner;
            float loser;

            if(dealerWinner == 0)
            {
                winner = playerPercentage;
                loser = dealerPercentage;
            }
            else
            {
                winner = dealerPercentage;
                loser = playerPercentage;
            }

            if (Math.Abs(winner - 100.0F) < 0.001) return 100.0F;
            if (Math.Abs(loser - 0.0F) < 0.001) return 100.0F;

            return 100.0F - loser;
        }

    }
}
