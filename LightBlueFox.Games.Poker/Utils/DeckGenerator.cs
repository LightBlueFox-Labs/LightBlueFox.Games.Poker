using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Utils
{
    public class DeckGenerator
    {
        private List<Card> cards;

        public Card PopRandom()
        {
            if (cards.Count == 0) throw new Exception("Cannot pop random since collection is empty");

            Random rnd = new Random();
            Card a = cards[rnd.Next(0, cards.Count - 1)];
            cards.Remove(a);
            return a;

        }

        public Card[] PopRandom(int nr)
        {
            Card[] cards = new Card[nr];
            for (int i = 0; i < nr; i++) cards[i] = PopRandom();
            return cards;
        }

        public DeckGenerator()
        {
            cards = new List<Card>();
            foreach (Suit suit in Enum.GetValues<Suit>())
            {
                foreach (CardValue val in Enum.GetValues<CardValue>())
                {
                    cards.Add(new Card(suit, val));
                }
            }
        }
    }
}
