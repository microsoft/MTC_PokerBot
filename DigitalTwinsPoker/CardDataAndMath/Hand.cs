using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardDataAndMath
{
    public class Hand: List<CardWrapper>
    {
        // we hold TargetHands as a definition in all hands
        static public Dictionary<string, int> TargetHands 
        { 
            get
            {
                return targetHands;
            }
        }

        static private readonly Dictionary<string, int> targetHands = new Dictionary<string, int>()
        {
            { "HighCard", 1 }
            , {"Pair", 2}
            , {"TwoPair", 3}
            , {"ThreeOfAKind", 4}
            , {"Straight", 5}
            , {"Flush", 6}
            , {"FullHouse", 7}
            , {"FourOfAKind", 8}
            , {"StraightFlush", 9} // royal flush is merely Aces high
        };

        static public Dictionary<int, string> ReverseTargetHands
        {
            get
            {
                return reverseTargetHands;
            }
        }

        static private readonly Dictionary<int, string> reverseTargetHands = new Dictionary<int, string>()
        {
            { 1, "High Card"}
            , {2, "Pair"}
            , {3, "Two Pair"}
            , {4, "Three of a Kind"}
            , {5, "Straight"}
            , {6, "Flush"}
            , {7, "Full House"}
            , {8, "Four of a Kind"}
            , {9, "Straight Flush"} // royal flush is merely Aces high
        };

        // the high cards are tie breakers, this does not necessarily duplicate the hand listing 
        public (int handType, int highCard, int secondHighCard, int thirdHighCard, int fourthHighCard, int fifthHighCard) Description { get; set; }
        public (int threeOfAKind, int straight, int flush, int fullHouse, int fourOfAKind, int straightFlush) Potentials { get; set; }

        public Hand()
        {
            Description = (0, 0, 0, 0, 0, 0);
            Potentials = (0, 0, 0, 0, 0, 0);
        }

        public Hand(Card card, bool commCard = false)
        {
            Description = (0, 0, 0, 0, 0, 0);
            Potentials = (0, 0, 0, 0, 0, 0);
            Add(card, commCard);
        }

        public Hand(Card card1, Card card2, bool commCard = false)
        {
            Description = (0, 0, 0, 0, 0, 0);
            Potentials = (0, 0, 0, 0, 0, 0);
            Add(card1, commCard);
            Add(card2, commCard);
        }

        public Hand(Hand hand, bool respectCommCards = true)
        {
            Description = (0, 0, 0, 0, 0, 0);
            Potentials = (0, 0, 0, 0, 0, 0);
            foreach(var h in hand)
            {
                if (respectCommCards == true && h.IsCommunityCard == true)
                    Add(h, true);
                else
                    Add(h, false);
            }
        }

        // Externally
        //
        // don't call Insert or InsertRange, you will also not maintain the correct order.
        // always call Add or AddRange
        public void Add(Card card, bool commCard = false) 
        {
            CardWrapper cw = new CardWrapper(card, commCard);

            int slot = Count;
            for (int i = 0; i < Count; ++i)
            {
                if (this[i].value > card.value)
                {
                    slot = i;
                    break;
                }
            }

            Insert(slot, cw);
        }

        public void Add(string name, bool commCard = false)
        {
            Card card = new Card(name, commCard);
            Add(card, commCard);
        }

        public List<CardWrapper> AddRange(IEnumerable<Card> cards, bool commCard = false)
        {
            foreach (var c in cards)
            {
                Add(c, commCard);
            }

            return this;
        }

        // do we's are special cases
        public (bool dowe, int val) DoWeHaveACommunityPair()
        {
            int cv = 0;
            List<int> pairsInCommCards = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int h = 0; h < this.Count; h++)
            {
                if (this[h].IsCommunityCard == true)
                    pairsInCommCards[this[h].value]++;
            }
            bool commPair = false;
            for (int picc = 0; picc < pairsInCommCards.Count; picc++)
            {
                if (pairsInCommCards[picc] == 2)
                {
                    commPair = true;
                    cv = picc;
                    break;
                }
            }

            return (commPair, cv);
        }

        public (bool dowe, int val) DoWeHaveACommunityThreeOfAKind()
        {
            int cv = 0;
            List<int> pairsInCommCards = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int h = 0; h < this.Count; h++)
            {
                if (this[h].IsCommunityCard == true)
                    pairsInCommCards[this[h].value]++;
            }
            bool commTrips = false;
            for (int picc = 0; picc < pairsInCommCards.Count; picc++)
            {
                if (pairsInCommCards[picc] == 3)
                {
                    commTrips = true;
                    cv = picc;
                    break;
                }
            }

            return (commTrips, cv);
        }

        public (bool dowe, int val, int val2) DoWeHaveAPotentialCommunityStraight()
        {
            List<int> straightValues;
            int cv1 = 0;
            int cv2 = 0;
            bool straight = false;

            for (int c = 5; c < 15; ++c)
            {
                straightValues = new List<int>();
                for (int st = c; st > c - 5; st--)
                {
                    if (st == 1) straightValues.Add(14);
                    else straightValues.Add(st);
                }

                int numFound = 5;
                int numCom = 0;

                for (int h = 0; h < this.Count; h++)
                {
                    if (this[h].IsCommunityCard == false) continue;
                    numCom++;
                    for (int jj = straightValues.Count - 1; jj >= 0; jj--)
                    {
                        if (this[h].value == straightValues[jj])
                        {
                            numFound--;
                            straightValues.RemoveAt(jj);
                            break;
                        }
                    }
                }
    
                if (numFound == 5 - numCom)
                {
                    straight = true;
                    foreach (var sv in straightValues)
                    {
                        if (cv1 == 0) cv1 = sv;
                        else cv2 = sv;
                    }
                }
            }

            return (straight, cv1, cv2);
        }

        public (bool dowe, int val) DoWeHaveAPotentialCommunityFlush()
        {
            int cv = 0;
            List<int> suitesInCommCards = new List<int> { 0, 0, 0, 0};
            for (int h = 0; h < this.Count; h++)
            {
                if (this[h].IsCommunityCard == true)
                {
                    if (this[h].Suite == "Heart")
                        suitesInCommCards[0]++;
                    if (this[h].Suite == "Spade")
                        suitesInCommCards[1]++;
                    if (this[h].Suite == "Club")
                        suitesInCommCards[2]++;
                    if (this[h].Suite == "Diamond")
                        suitesInCommCards[3]++;
                }
            }
            bool flush = false;
            for (int picc = 0; picc < suitesInCommCards.Count; picc++)
            {
                if (suitesInCommCards[picc] == this.Count)
                {
                    flush = true;
                    cv = picc;
                    break;
                }
            }

            return (flush, cv);
        }

        public (bool dowe, int t, int p1, int p2) DoWeHaveAPotentialCommunityFullHouse()
        {
            int t = 0;
            int p1 = 0;
            int p2 = 0;
            List<int> pairsInCommCards = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int h = 0; h < this.Count; h++)
            {
                if (this[h].IsCommunityCard == true)
                    pairsInCommCards[this[h].value]++;
            }
            bool commFh;
            for (int picc = 0; picc < pairsInCommCards.Count; picc++)
            {
                if (pairsInCommCards[picc] == 3)
                {
                    t = picc;
                    break;
                }
                if (pairsInCommCards[picc] == 2)
                {
                    if(p1 == 0)
                    {
                        p1 = picc;
                    }
                    else
                    {
                        p2 = picc;
                        break;
                    }
                }
            }

            if (!(t != 0 || (p1 != 0 && p2 != 0)))
            {
                t = 0;
                p1 = 0;
                p2 = 0;
                commFh = false;
            }
            else
                commFh = true;

            return (commFh, t, p1, p2);
        }

        public (bool dowe, int val1, int val2, int val3) DoWeHaveAPotentialCommunityStraightFlush()
        {
            List<int> straightValues;
            int cv1 = 0;
            int cv2 = 0;
            int suite = 0;
            bool straight = false;
            bool done = false;

            for (int s = 0; s < 4; ++s)
            {
                for (int c = 5; c < 15; ++c)
                {
                    straightValues = new List<int>();
                    for (int st = c; st > c - 5; st--)
                    {
                        if (st == 1) straightValues.Add(14);
                        else straightValues.Add(st);
                    }

                    int numFound = 5;
                    int numCom = 0;

                    for (int h = 0; h < this.Count; h++)
                    {
                        if (this[h].IsCommunityCard == false) continue;
                        numCom++;
                        for (int jj = straightValues.Count - 1; jj >= 0; jj--)
                        {
                            if (this[h].value == straightValues[jj] && this[h].Suite == Card.CardSuites[s])
                            {
                                numFound--;
                                straightValues.RemoveAt(jj);
                                break;
                            }
                        }
                    }

                    if (numFound == 5 - numCom)
                    {
                        straight = true;
                        foreach (var sv in straightValues)
                        {
                            if (cv1 == 0) cv1 = sv;
                            else if(cv2 == 0) cv2 = sv;
                        }
                        suite = s;
                        done = true;
                        break;
                    }
                }
                if (done == true)
                    break;
            }

            return (straight, suite, cv1, cv2);
        }

        // Tuples are target hand type, high card value, second high card value for tie breakers
        // List will be sorted ascending with [0] being the current hand with no better cards

        // the full description in written to the hand
        public int CalculateHand(bool ignoreCommunityPlacement = false)
        {
            int handType;
            int highCard;
            int secondHighCard;
            int thirdHighCard;
            int fourthHighCard;
            int fifthHighCard;

            // if this is the end, we do not care where the cards are
            if (this.Count >= 7) ignoreCommunityPlacement = true;

            int ret, ret1, ret2, ret3, ret4;
            // straight flush
            (ret, ret1, ret2, ret3, ret4) = this.IsStraight(true, ignoreCommunityPlacement);
            if (ret > 0)
            {
                (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["StraightFlush"], ret, ret1, ret2, ret3, ret4);
            }
            else
            {
                (ret, ret1) = this.IsFourOfAKind(ignoreCommunityPlacement);
                if (ret > 0)
                {
                    (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["FourOfAKind"], ret, ret1, 0, 0, 0);
                }
                else
                {
                    (ret, ret2) = this.IsFullHouse(ignoreCommunityPlacement);
                    if (ret != 0)
                    {
                        (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["FullHouse"], ret, ret2, 0, 0, 0);
                    }
                    else
                    {
                        (ret, ret1, ret2, ret3, ret4) = this.IsFlush(ignoreCommunityPlacement);
                        if (ret != 0)
                        {
                            (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["Flush"], ret, ret1, ret2, ret3, ret4);
                        }
                        else
                        {
                            (ret, ret1, ret2, ret3, ret4) = this.IsStraight(false, ignoreCommunityPlacement);
                            if (ret != 0)
                            {
                                (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["Straight"], ret, ret1, ret2, ret3, ret4);

                            }
                            else
                            {
                                (ret, ret1, ret2) = this.IsThreeOfAKind(ignoreCommunityPlacement);
                                if (ret != 0)
                                {
                                    (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["ThreeOfAKind"], ret, ret1, ret2, 0, 0);

                                }
                                else
                                {
                                    (ret, ret1, ret2) = this.IsTwoPairs(ignoreCommunityPlacement);
                                    if (ret != 0)
                                    {
                                        (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["TwoPair"], ret, ret1, ret2, 0, 0);
                                    }
                                    else
                                    {
                                        (ret, ret1, ret2, ret3) = this.IsOnePair(ignoreCommunityPlacement);
                                        if (ret != 0)
                                        {
                                            (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["Pair"], ret, ret1, ret2, ret3, 0);
                                        }
                                        else
                                        {
                                            (ret, ret1, ret2, ret3, ret4) = this.DescribeHighCards(ignoreCommunityPlacement);
                                            (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard) = new Tuple<int, int, int, int, int, int>(Hand.TargetHands["HighCard"], ret, ret1, ret2, ret3, ret4);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            this.Description = (handType, highCard, secondHighCard, thirdHighCard, fourthHighCard, fifthHighCard);

            return handType;
        }


        //these are the hand determination functions
        //we are going to call this from all the others in order to get the full high card set, which is why 
        //doNotReturnTheseValues was implemented
        public (int high1, int high2, int high3, int high4, int high5) DescribeHighCards(bool ignoreCommunityPlacement, List<int> doNotReturnTheseValues = null)
        {
            List<Card> high = new List<Card>();
            if (doNotReturnTheseValues == null) doNotReturnTheseValues = new List<int>();

            for (int i = this.Count - 1; i >= 0; i--)
            {
                Card c = new Card(this[i].Name, this[i].IsCommunityCard);
                bool match = false;
                foreach(var dn in doNotReturnTheseValues)
                {
                    if (dn == c.value)
                    {
                        match = true;
                        break;
                    }
                }
                if(match == false) high.Add(c);
            }

            Card cache;
            // we want the community cards at the end
            if (ignoreCommunityPlacement == false)
            {
                for (int j = 0; j < high.Count - 1; j++)
                {
                    if (high[j].IsCommunityCard == true)
                    {
                        for (int i = j + 1; i < high.Count; i++)
                        {
                            if (high[i].IsCommunityCard == false)
                            {
                                for (int k = 0; k < i - j; ++k)
                                {
                                    cache = high[i - k];
                                    high[i - k] = high[i - k - 1];
                                    high[i - k - 1] = cache;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            List<int> highValues = new List<int>();
            foreach (var h in high) highValues.Add(h.value);
            while (highValues.Count < 5) highValues.Add(0);

            return (highValues[0], highValues[1], highValues[2], highValues[3], highValues[4]);
        }

        private (int [] cardValueArray, int [] cardValueArrayNonComm) SetUpCardArrays(bool ignoreCommunityPlacement)
        {
            int[] cardValueArray = new int[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] cardValueArrayNonComm = new int[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int c = 0; c < this.Count; c++)
            {
                cardValueArray[this[c].value]++;
                if (this[c].IsCommunityCard == false || ignoreCommunityPlacement == true)
                    cardValueArrayNonComm[this[c].value]++;
            }

            return (cardValueArray, cardValueArrayNonComm);
        }

        public (int pair, int high1, int high2, int high3) IsOnePair(bool ignoreCommunityPlacement)
        {
            (int[] cardValueArray, int[] cardValueArrayNonComm) = SetUpCardArrays(ignoreCommunityPlacement);

            int pair = 0;
            for (int i = 14; i > 1; --i)
            {
                // we do not care about community pairs
                if (cardValueArray[i] == 2 && cardValueArrayNonComm[i] > 0)
                {
                    pair = i;
                    break;
                }
            }

            if(pair == 0)
                return (0, 0, 0 ,0);

            (int high1, int high2, int high3, _, _) = DescribeHighCards(ignoreCommunityPlacement, new List<int>() { pair });

            return (pair, high1, high2, high3);
        }

        public (int pairh, int pairl, int high1) IsTwoPairs(bool ignoreCommunityPlacement)
        {
            (int[] cardValueArray, int[] cardValueArrayNonComm) = SetUpCardArrays(ignoreCommunityPlacement);

            int pairh = 0;
            int pairl = 0;
            for (int i = 14; i > 1; --i)
            {
                if (cardValueArray[i] == 2 && cardValueArrayNonComm[i] > 0)
                {
                    if (pairh == 0)
                        pairh = i;
                    else
                    {
                        pairl = i;
                        // you can have 3 pairs
                        break;
                    }
                }
            }
            if (pairh == 0 || pairl == 0)
                return (0, 0, 0);

            (int high1, _, _, _, _) = DescribeHighCards(ignoreCommunityPlacement, new List<int>() { pairh, pairl });

            return (pairh, pairl, high1);
        }


        public (int trip, int high1, int high2) IsThreeOfAKind(bool ignoreCommunityPlacement)
        {
            (int[] cardValueArray, int[] cardValueArrayNonComm) = SetUpCardArrays(ignoreCommunityPlacement);

            int trip = 0;

            for (int i = 14; i > 1; i--)
            {
                if (cardValueArray[i] == 3 && cardValueArrayNonComm[i] > 0)
                {
                    trip = i;
                    break;
                }
            }
            if (trip == 0)
                return (0, 0, 0);

            (int high1, int high2, _, _, _) = DescribeHighCards(ignoreCommunityPlacement, new List<int>() { trip });

            return (trip, high1, high2);
        }

        private static IEnumerable<T[]> CombinationsOfFive<T>(IEnumerable<T> source)
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

        // if straightFlush == true it looks for a straightFlush, else it looks for a straight
        public (int high1, int high2, int high3, int high4, int high5) IsStraight(bool straightFlush, bool ignoreCommunityPlacement)
        {
            int[] cstarts = null;
            List<Hand> hands = new List<Hand>();

            if (this.Count == 6)
            {
                cstarts = new int[] { 0, 1, 2, 3, 4, 5 };
            }
            else if (this.Count == 7)
            {
                cstarts = new int[] { 0, 1, 2, 3, 4, 5, 6 };
            }

            if (this.Count <= 5)
            {
                hands.Add(this);
            }
            else
            {
                var combos = CombinationsOfFive<int>(cstarts);
                foreach (var i in combos)
                {
                    if (i.Count() != 5) continue;
                    Hand p = new Hand();
                    foreach (var h in i)
                    {
                        p.Add(this[h], this[h].IsCommunityCard);
                    }
                    hands.Add(p);
                }
            }

            int high1=0, high2=0, high3=0, high4=0, high5=0;

            foreach (var h in hands)
            {
                int ret = 0;
                if (straightFlush == false)
                {
                    if (h.Count == 5 &&
                        ((h[4].value - 1 == h[3].value
                        && h[3].value - 1 == h[2].value
                        && h[2].value - 1 == h[1].value
                        && h[1].value - 1 == h[0].value) ||
                            (h[3].value - 1 == h[2].value
                        && h[2].value - 1 == h[1].value
                        && h[1].value - 1 == h[0].value
                        && h[0].value == 2
                        && h[4].value == 14))
                        )
                        ret = h[4].value;
                }
                else
                {
                    if (h.Count == 5 &&
                       ((h[4].value - 1 == h[3].value && h[4].Suite == h[3].Suite
                       && h[3].value - 1 == h[2].value && h[3].Suite == h[2].Suite
                       && h[2].value - 1 == h[1].value && h[2].Suite == h[1].Suite
                       && h[1].value - 1 == h[0].value && h[1].Suite == h[0].Suite) ||
                           (h[3].value - 1 == h[2].value && h[3].Suite == h[2].Suite
                       && h[2].value - 1 == h[1].value && h[2].Suite == h[1].Suite
                       && h[1].value - 1 == h[0].value && h[1].Suite == h[0].Suite
                       && h[0].value == 2
                       && h[4].value == 14 && h[4].Suite == h[3].Suite))
                       )
                        ret = h[4].value;
                }

                if (ignoreCommunityPlacement == false
                    && ret != 0
                    && h[0].IsCommunityCard == true
                    && h[1].IsCommunityCard == true
                    && h[2].IsCommunityCard == true
                    && h[3].IsCommunityCard == true
                    && h[4].IsCommunityCard == true)
                    ret = 0;

                if(ret != 0 && h[4].value > high1)
                {
                    high1 = h[4].value;
                    high2 = h[3].value;
                    high3 = h[2].value;
                    high4 = h[1].value;
                    high5 = h[0].value;
                }
            }

            return (high1, high2, high3, high4, high5);
        }

        public (int high1, int high2, int high3, int high4, int high5) IsFlush(bool ignoreCommunityPlacement)
        {
            int[] cardSuiteArray = new int[4] { 0, 0, 0, 0 };
            int[] cardSuiteArrayNonComm = new int[4] { 0, 0, 0, 0 };

            for (int c = 0; c < this.Count; c++)
            {
                switch (this[c].Suite)
                {
                    case "Heart":
                        cardSuiteArray[0]++;
                        if (ignoreCommunityPlacement == false || this[c].IsCommunityCard == false)
                            cardSuiteArrayNonComm[0]++;
                        break;
                    case "Spade":
                        cardSuiteArray[1]++;
                        if (ignoreCommunityPlacement == false || this[c].IsCommunityCard == false)
                            cardSuiteArrayNonComm[1]++;
                        break;
                    case "Club":
                        cardSuiteArray[2]++;
                        if (ignoreCommunityPlacement == false || this[c].IsCommunityCard == false)
                            cardSuiteArrayNonComm[2]++;
                        break;
                    default:
                        cardSuiteArray[3]++;
                        if (ignoreCommunityPlacement == false || this[c].IsCommunityCard == false)
                            cardSuiteArrayNonComm[3]++;
                        break;
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                if (cardSuiteArray[i] >= 5 && (cardSuiteArrayNonComm[i] >= 1 || ignoreCommunityPlacement == true))
                {
                    // because of pairs we'll create a new hand to sort
                    Hand flushOnlyHand = new Hand();
                    foreach (var c in this)
                    {
                        if (c.Suite == Card.CardSuites[i])
                        {
                            flushOnlyHand.Add(c, false);
                        }
                    }

                    return (flushOnlyHand[^1].value, flushOnlyHand[^2].value, flushOnlyHand[^3].value, flushOnlyHand[^4].value, flushOnlyHand[^5].value);
                }
            }

            return (0, 0, 0, 0, 0);

        }

        public (int trips, int pair) IsFullHouse(bool ignoreCommunityPlacement)
        {
            (int[] cardValueArray, int[] cardValueArrayNonComm) = SetUpCardArrays(ignoreCommunityPlacement);

            int trips = 0;
            int pair = 0;
            for (int i = 2; i < 15; ++i)
            {
                if (cardValueArray[i] == 3)
                    trips = i;
                else if (cardValueArray[i] == 2)
                    pair = i;
            }
            if (trips == 0 || pair == 0 || (cardValueArrayNonComm[trips] == 0 && cardValueArrayNonComm[pair] == 0))
            {
                // all or none
                trips = 0;
                pair = 0;
            }

            return (trips, pair);
        }

        public (int four, int one) IsFourOfAKind(bool ignoreCommunityPlacement)
        {
            (int[] cardValueArray, int[] cardValueArrayNonComm) = SetUpCardArrays(ignoreCommunityPlacement);

            int four = 0;
            int one = 0;
            for (int i = 2; i < 15; ++i)
            {
                if (cardValueArray[i] == 4)
                    four = i;
                else if (cardValueArray[i] == 1)
                    one = i;
            }
            if (four == 0 || cardValueArrayNonComm[four] == 0)
            {
                four = 0;
                one = 0;
            }

            return (four, one);
        }
    }
}
