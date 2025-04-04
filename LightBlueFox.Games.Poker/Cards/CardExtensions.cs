using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Cards
{
    public static class CardExtentions
    {
        public static string Print(this IEnumerable<Card> cards, string seperator = "")
            => cards.Select(c => c.ToString()).Aggregate((s, t) => s + seperator + t);

        public static bool SameSuit(this IEnumerable<Card> cards)
            => !cards.Any() || cards.All(c => cards.First().Suit == c.Suit);

        public static bool AreOfValues(this IEnumerable<Card> cards, IEnumerable<CardValue> values)
            => cards.Select(c => c.Value).Order().SequenceEqual(values.Order());

        public static IEnumerable<Card> GetLongestSequence(this IEnumerable<Card> cards, int? minLength = null)
        {
            cards = cards.OrderDescending().DistinctBy(c => c.Value);

            int bestSequenceLength = -1;
            int bestSequenceStart = -1;

            int sequenceStart = 0;
            CardValue? lastValue = null;
            foreach (var (card, index) in cards.Select((c, i) => (c, i)))
            {
                if (lastValue == null || card.Value - lastValue == -1)
                {
                    int len = index - sequenceStart + 1;
                    if (bestSequenceLength < 0 || len > bestSequenceLength)
                    {
                        bestSequenceLength = len;
                        bestSequenceStart = sequenceStart;
                    }
                }
                else
                {
                    sequenceStart = index;
                }
                lastValue = card.Value;
            }

            return bestSequenceStart >= 0 && (minLength == null || bestSequenceLength >= minLength) ? cards.Skip(bestSequenceStart).Take(bestSequenceLength) : [];

        }

        public static CardValue HighestValue(this IEnumerable<Card> cards)
            => cards.Any() ? cards.OrderDescending().FirstOrDefault().Value : throw new ArgumentException("Empty cards!");

        public static bool ContainsCardValue(this IEnumerable<Card> cards, CardValue val)
            => cards.Any(x => x.Value == val);
    }
}
