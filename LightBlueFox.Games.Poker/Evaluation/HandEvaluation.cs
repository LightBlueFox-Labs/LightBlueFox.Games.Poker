using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker.Evaluation
{
    public record class HandEvaluation(Card[] TableCards, Card[] PlayerCards, Card[] Hand, PokerHandType Type) : IComparable<HandEvaluation>
    {
        public int CompareTo(HandEvaluation? other)
        {
            ArgumentNullException.ThrowIfNull(other, "No reasonable comparison for null type!");
            return Type == other.Type ? CompareWithKicker(other) : Type.CompareTo(other.Type);
        }

        public int CompareWithKicker(HandEvaluation other)
        {
            if (Type.Comparator(Hand, other.Hand) is int handComp && handComp != 0) return handComp;

            if (Hand.Length != other.Hand.Length) throw new InvalidOperationException("This should never happen!");
            int remCardsLen = 5 - Hand.Length;
            if (remCardsLen <= 0) return 0;

            var r1 = TableCards.Concat(PlayerCards).Except(Hand).OrderByDescending(c => c.Value);
			var r2 = other.TableCards.Concat(other.PlayerCards).Except(other.Hand).OrderByDescending(c => c.Value);

			for (int i = 0; i < remCardsLen; i++) {
                if (r1.ElementAt(i).CompareTo(r2.ElementAt(i)) is int kickerComp && kickerComp != 0) return kickerComp;
            }
            return 0;
        }

        public static HandEvaluation Evaluate(Card[] table, Card[] playerCards)
        {    
            var (type, cards) = PokerHandType.Evaluate([..table, ..playerCards]);
            return new(table, playerCards, cards, type);
        }    

		public override string ToString()
		{
			return Type.Name;
		}
	}
}
