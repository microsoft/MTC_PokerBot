using System;
using System.Text;

namespace CardDataAndMath
{
    public class Card
    {
        static private string[] cardValueNames = new string[15] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };
        static private string[] cardValues = new string[15] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        static private string[] cardSuites = new string[4] { "Heart", "Spade", "Club", "Diamond" };

        static public string[] CardValueNames
        {
            get { return cardValueNames; }
        }

        static public string[] CardValues 
        {
            get { return cardValues; }
        }

        static public string[] CardSuites
        {
            get { return cardSuites; }
        }

        public int value;
        public string Value { get; }
        public string Suite { get; }
        public string Color { get; }
        public string Name { get; }
        public bool IsCommunityCard { get; }

        public Card(Card card, bool commCard = false)
        {
            value = card.value;
            Value = card.Value;
            Suite = card.Suite;
            Color = card.Color;
            Name = card.Name;
            // on purpose we do not take this value from the existing card
            IsCommunityCard = commCard;
        }

        // name format examples: KS 10H 9D 8C
        public Card(string name, bool commCard = false)
        {
            if (name.Length < 2 || name.Length > 3)
                throw new ArgumentException("Incorrect card name");

            Name = name.ToUpperInvariant();
            IsCommunityCard = commCard;

            switch (Name[Name.Length - 1])
            {
                case 'S':
                    Suite = "Spade";
                    Color = "Black";
                    break;
                case 'H':
                    Suite = "Heart";
                    Color = "Red";
                    break;
                case 'D':
                    Suite = "Diamond";
                    Color = "Red";
                    break;
                case 'C':
                    Suite = "Club";
                    Color = "Black";
                    break;
                default:
                    throw new ArgumentException("Incorrect Card Suite");
            }

            bool parseSuccess = false;
            switch (Name[0])
            {
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    Value = Name[0].ToString();
                    value = int.Parse(Value);
                    parseSuccess = true;
                    break;
                case 'J':
                    Value = "Jack";
                    value = 11;
                    parseSuccess = true;
                    break;
                case 'Q':
                    Value = "Queen";
                    value = 12;
                    parseSuccess = true;
                    break;
                case 'K':
                    Value = "King";
                    value = 13;
                    parseSuccess = true;
                    break;
                case 'A':
                    Value = "Ace";
                    value = 14; 
                    parseSuccess = true;
                    break;
            }
            if (Name[0] == '1')
            {
                if (Name.Length < 3)
                    throw new ArgumentException("Incorrect Card Value");
                Value = "10";
                value = int.Parse(Value);
                parseSuccess = true;
            }

            if(parseSuccess == false)
                throw new ArgumentException("Incorrect Card Value");
        }

        public Card(string ValueName, string Suite, bool commCard = false)
        {
            StringBuilder nameString = new StringBuilder();
            if (ValueName.ToUpperInvariant() == "JACK"
                || ValueName.ToUpperInvariant() == "QUEEN"
                || ValueName.ToUpperInvariant() == "KING"
                || ValueName.ToUpperInvariant() == "ACE")
                nameString.Append(ValueName.ToUpperInvariant()[0]);
            else
                nameString.Append(ValueName);
            nameString.Append(Suite.ToUpperInvariant()[0]);

            if (nameString.ToString().Length < 2 || nameString.ToString().Length > 3)
                throw new ArgumentException("Incorrect card name");

            Name = nameString.ToString().ToUpperInvariant();
            IsCommunityCard = commCard;

            switch (Name[Name.Length - 1])
            {
                case 'S':
                    Suite = "Spade";
                    Color = "Black";
                    break;
                case 'H':
                    Suite = "Heart";
                    Color = "Red";
                    break;
                case 'D':
                    Suite = "Diamond";
                    Color = "Red";
                    break;
                case 'C':
                    Suite = "Club";
                    Color = "Black";
                    break;
                default:
                    throw new ArgumentException("Incorrect Card Suite");
            }

            bool parseSuccess = false;
            switch (Name[0])
            {
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    Value = Name[0].ToString();
                    value = int.Parse(Value);
                    parseSuccess = true;
                    break;
                case 'J':
                    Value = "Jack";
                    value = 11;
                    parseSuccess = true;
                    break;
                case 'Q':
                    Value = "Queen";
                    value = 12;
                    parseSuccess = true;
                    break;
                case 'K':
                    Value = "King";
                    value = 13;
                    parseSuccess = true;
                    break;
                case 'A':
                    Value = "Ace";
                    value = 14;
                    parseSuccess = true;
                    break;
            }
            if (Name[0] == '1')
            {
                if (Name.Length < 3)
                    throw new ArgumentException("Incorrect Card Value");
                Value = "10";
                value = int.Parse(Value);
                parseSuccess = true;
            }

            if (parseSuccess == false)
                throw new ArgumentException("Incorrect Card Value");

        }


        public override bool Equals(object obj)
        {
            bool res = obj is CardWrapper cardw &&
                   value == cardw.value &&
                   Suite == cardw.Suite;

            bool res2 = obj is Card card &&
               value == card.value &&
               Suite == card.Suite;

            if (res == true || res2 == true)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value, Suite);
        }

        public static bool operator ==(Card obj1, Card obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(Card obj1, Card obj2)
        {
            return !(obj1 == obj2);
        }

        public static string CreateName(string ValueName, string Suite)
        {
            ValueName = ValueName.ToUpperInvariant();
            Suite = Suite.ToUpperInvariant();

            if (!(
                ValueName == "2"
                || ValueName == "3"
                || ValueName == "4"
                || ValueName == "5"
                || ValueName == "6"
                || ValueName == "7"
                || ValueName == "8"
                || ValueName == "9"
                || ValueName == "10"
                || ValueName == "JACK"
                || ValueName == "QUEEN"
                || ValueName == "KING"
                || ValueName == "ACE"
                )) throw new ArgumentException("Incorrect card ValueName");

            if (!(
                Suite == "HEART"
                || Suite == "CLUB"
                || Suite == "DIAMOND"
                || Suite == "SPADE"
                )) throw new ArgumentException("Incorrect card Suite");

            if (ValueName == "JACK")
                ValueName = "J";
            else if (ValueName == "QUEEN")
                ValueName = "Q";
            else if (ValueName == "KING")
                ValueName = "K";
            else if (ValueName == "ACE")
                ValueName = "A";

            return ValueName + Suite[0];
        }
    }
}