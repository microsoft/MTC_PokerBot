using System;
using System.Collections.Generic;
using System.Text;

namespace CardDataAndMath
{
    // this probably is not used

    public class Deck : List<CardWrapper>
    {
        public Deck()
        {
        }

        public void ShuffleDeck()
        {
            var rand = new Random();
            int deckLength = Count;

            while(deckLength > 0)
            {
                var nc = rand.Next(0, deckLength--);
                AddCardToEndOfDeck(this[nc].Name);
                RemoveAt(nc);
            }
            for (int c = 0; c < Count; ++c)
                this[c].Order = c;

        }

        public void CreateNewDeck()
        {
            AddCardToEndOfDeck("AS");
            AddCardToEndOfDeck("2S");
            AddCardToEndOfDeck("3S");
            AddCardToEndOfDeck("4S");
            AddCardToEndOfDeck("5S");
            AddCardToEndOfDeck("6S");
            AddCardToEndOfDeck("7S");
            AddCardToEndOfDeck("8S");
            AddCardToEndOfDeck("9S");
            AddCardToEndOfDeck("10S");
            AddCardToEndOfDeck("JS");
            AddCardToEndOfDeck("QS");
            AddCardToEndOfDeck("KS");

            AddCardToEndOfDeck("AH");
            AddCardToEndOfDeck("2H");
            AddCardToEndOfDeck("3H");
            AddCardToEndOfDeck("4H");
            AddCardToEndOfDeck("5H");
            AddCardToEndOfDeck("6H");
            AddCardToEndOfDeck("7H");
            AddCardToEndOfDeck("8H");
            AddCardToEndOfDeck("9H");
            AddCardToEndOfDeck("10H");
            AddCardToEndOfDeck("JH");
            AddCardToEndOfDeck("QH");
            AddCardToEndOfDeck("KH");

            AddCardToEndOfDeck("AC");
            AddCardToEndOfDeck("2C");
            AddCardToEndOfDeck("3C");
            AddCardToEndOfDeck("4C");
            AddCardToEndOfDeck("5C");
            AddCardToEndOfDeck("6C");
            AddCardToEndOfDeck("7C");
            AddCardToEndOfDeck("8C");
            AddCardToEndOfDeck("9C");
            AddCardToEndOfDeck("10C");
            AddCardToEndOfDeck("JC");
            AddCardToEndOfDeck("QC");
            AddCardToEndOfDeck("KC");

            AddCardToEndOfDeck("AD");
            AddCardToEndOfDeck("2D");
            AddCardToEndOfDeck("3D");
            AddCardToEndOfDeck("4D");
            AddCardToEndOfDeck("5D");
            AddCardToEndOfDeck("6D");
            AddCardToEndOfDeck("7D");
            AddCardToEndOfDeck("8D");
            AddCardToEndOfDeck("9D");
            AddCardToEndOfDeck("10D");
            AddCardToEndOfDeck("JD");
            AddCardToEndOfDeck("QD");
            AddCardToEndOfDeck("KD");
        }

        private void AddCardToEndOfDeck(string cardName)
        {
            Add(new CardWrapper(new Card(cardName)));
            this[Count - 1].Order = Count - 1;
        }
    }
}
