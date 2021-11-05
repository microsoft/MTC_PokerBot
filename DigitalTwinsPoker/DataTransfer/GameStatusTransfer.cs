using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransfer
{
    public class GameStatusTransfer
    {
        public Game Game { get; set; }
        public PlayerHand Player { get; set; }
        public PlayerHand Dealer { get; set; }
        public CommunityHand Community { get; set; }
        public HandsDisplay Display { get; set; }
    }

    public class HandsDisplay
    {
        // there are 4
        public string [] DisplayStrings { get; set; }
    }

    public class Game
    {
        public string PotentialWinner { get; set; }
        public string ProbabilityOfWinning { get; set; }
    }

    // dealer is a player
    public class PlayerHand
    {
        public string Name { get; set; }
        public string ProbabilityOfWinning { get; set; }
        public string HandType { get; set; }
        public string HighCard { get; set; }
        public string SecondHighCard { get; set; }
        public string ThirdHighCard { get; set; }
        public string FourthHighCard { get; set; }
        public string FifthHighCard { get; set; }
        public string[] Potentials { get; set; }
        public DtCard[] Cards { get; set; }
        public string Descriptor { get; set; }
    }

    public class CommunityHand
    {
        public DtCard[] Cards { get; set; }
    }

    public class DtCard
    {
        public string Value { get; set; }
        public string Suite { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
    }
}
