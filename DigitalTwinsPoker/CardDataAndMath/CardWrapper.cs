using System;
using System.Collections.Generic;
using System.Text;

namespace CardDataAndMath
{
    public class CardWrapper: Card
    {
        public int Order { get; set; }


        public CardWrapper(Card card) : base(card.Name, false) { }

        public CardWrapper(string cardName) : base(cardName, false) { }

        public CardWrapper(Card card, bool commCard) : base(card.Name, commCard) { }

        public CardWrapper(string cardName, bool commCard) : base(cardName, commCard) { }

        public override bool Equals(object obj)
        {
            bool res =  obj is CardWrapper cardw &&
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

        public static bool operator ==(CardWrapper obj1, CardWrapper obj2)
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

        public static bool operator !=(CardWrapper obj1, CardWrapper obj2)
        {
            return !(obj1 == obj2);
        }

    }
}
