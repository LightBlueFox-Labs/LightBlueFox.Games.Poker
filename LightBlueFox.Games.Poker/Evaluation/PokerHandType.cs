using LightBlueFox.Games.Poker.Cards;

namespace LightBlueFox.Games.Poker.Evaluation
{
    /// <summary>
    /// Checks if a certain type of hand can be formed from the given cards. If so, returns the best combination of cards of the type possible.
    /// </summary>
    public delegate IEnumerable<Card>? IsHandOfType(IEnumerable<Card> cards);
	
	public delegate int Compare(IEnumerable<Card> c1, IEnumerable<Card> c2);


	public class PokerHandType : IComparable<PokerHandType>
	{
		public readonly string Name;
		public readonly IsHandOfType IsOfType;
		public readonly Compare Comparator;

		private PokerHandType(string name, IsHandOfType isOfType, Compare comparator)
		{
			Name = name;
			IsOfType = isOfType;
			Comparator = comparator;
		}


		public bool IsAnyStraight
			=> this == Straight || this == StraightFlush || this == RoyalFlush;

		public override string ToString() => Name;
		public static (PokerHandType Type, Card[] Cards) Evaluate(IEnumerable<Card> cards)
			=> TypesDescending
			.Select(t => (t, t.IsOfType(cards)))
			.FirstOrDefault(c => c.Item2 is not null) is (PokerHandType type, IEnumerable<Card> c) ? new(type, [.. c])
			: throw new InvalidOperationException("No matching hand found!");

		private static Dictionary<PokerHandType, int>? _handTyleRankings;
		private static Dictionary<PokerHandType, int> HandTypeRankings => _handTyleRankings ??= TypesDescending.Select((h, i) => (h, i)).ToDictionary();

		

		public int CompareTo(PokerHandType? other)
		{
			ArgumentNullException.ThrowIfNull(other, "No reasonable comparison for null type!");
			// Lower Ranking => Stronger Hand!
			return -1 * HandTypeRankings[this].CompareTo(HandTypeRankings[other]);
		}

		#region Hand Implementations
		public readonly static PokerHandType HighCard = new(
				"High Card",
				// Always Matches. Return highest card.
				(c) => [c.OrderByDescending(c => c.Value).First()],
				// Orders again to be sure.
				(c1, c2) =>
				{
					c1 = c1.OrderByDescending(c => c.Value);
					c2 = c2.OrderByDescending(c => c.Value);
					for (var i = 0; i < int.Max(c1.Count(), c2.Count()); i++)
					{
						if (c1.ElementAtOrDefault(i).CompareTo(c1.ElementAtOrDefault(i)) is int res && res != 0) return res;
					}
					return 0;
				}
		);

		public readonly static PokerHandType Pair = new(
				"Pair",
				(IEnumerable<Card> cards) =>
					cards.GroupBy(c => c.Value)
					.Where(s => s.Count() == 2)
					.OrderByDescending(s => s.First().Value)
					.FirstOrDefault()
				,
				(c1, c2) => c1.HighestValue().CompareTo(c2.HighestValue())
		);

		public readonly static PokerHandType TwoPair = new(
				"Two Pair",
				(IEnumerable<Card> cards) =>
					cards.GroupBy(c => c.Value)
					.Where(s => s.Count() == 2)
					.OrderByDescending(s => s.First().Value) is var grps &&
						grps.Count() >= 2 ?
						grps.Take(2).SelectMany(g => g)
						: null
				,
				(c1, c2) => c1.OrderDescending().ElementAt(0).Value.CompareTo(c2.OrderDescending().ElementAt(0).Value) is int bpRes && bpRes != 0 ? bpRes 
				: c1.OrderDescending().ElementAt(2).Value.CompareTo(c2.OrderDescending().ElementAt(2).Value)
		);

		public readonly static PokerHandType ThreeOfAKind = new(
			"Three of a kind",
				(IEnumerable<Card> cards) =>
					cards.GroupBy(c => c.Value)
					.Where(s => s.Count() == 3)
					.OrderByDescending(s => s.First().Value)
					.FirstOrDefault()
				,
				(c1, c2) => c1.HighestValue().CompareTo(c2.HighestValue())
		);

		/// <summary>
		/// A straight. Will always be ordered descending.
		/// </summary>
		public readonly static PokerHandType Straight = new(
				"Straight",
				(IEnumerable<Card> cards) =>
				{
					//throw new NotImplementedException("This is incorrectly implemented. GetLongestSequence returns incorrect values (single card instead of sequence!). Also edge case where cards are 6 5 4 3 2 1 A ");
					IOrderedEnumerable<Card>? sequence = cards.GetLongestSequence(4).OrderDescending();
					if (sequence == null) return null;

					if (sequence.Count() == 4 && sequence.Take(4).AreOfValues([CardValue.Five, CardValue.Four, CardValue.Three, CardValue.Two]) && cards.ContainsCardValue(CardValue.Ace))
						return sequence.Take(4).Append(cards.First(c => c.Value == CardValue.Ace));

					return sequence.Count() >= 5 ? sequence.Take(5) : null;
				},
				(c1, c2) => (c1.Select(c => c.Value).OrderDescending().SequenceEqual([CardValue.Ace, CardValue.Five, CardValue.Four, CardValue.Three, CardValue.Two]) ? CardValue.Five : c1.HighestValue())
				.CompareTo(c2.Select(c => c.Value).OrderDescending().SequenceEqual([CardValue.Ace, CardValue.Five, CardValue.Four, CardValue.Three, CardValue.Two]) ? CardValue.Five : c2.HighestValue())
		);

		public readonly static PokerHandType Flush = new(
			"Flush",
			(IEnumerable<Card> cards) =>
				new Suit[4] { Suit.Clubs, Suit.Spades, Suit.Diamonds, Suit.Hearts }.Select(
						s =>
							cards.OrderByDescending(c => c.Value).Where(c => c.Suit == s)
						).Where(s => s.Count() >= 5)
			.OrderByDescending(s => s.HighestValue())
			.FirstOrDefault()?
			.Take(5)
			,
			(c1, c2) => c1.HighestValue().CompareTo(c2.HighestValue())
		);

		public readonly static PokerHandType FullHouse = new(
				"Full House",

				(IEnumerable<Card> cards) =>
					cards.GroupBy(c => c.Value)
				.OrderByDescending(g => g.First().Value) is var groups
				&& groups.FirstOrDefault(g => g.Count() >= 3) is IGrouping<CardValue, Card> big
				&& groups.FirstOrDefault(g => g.Key != big.Key && g.Count() >= 2) is IGrouping<CardValue, Card> small
				? big.Concat(small).ToList() : null,

				(c1, c2) => c1.GroupBy(c => c.Value).GroupBy(s => s.Count()).ToDictionary(g => g.Key) is var d1
							&& c2.GroupBy(c => c.Value).GroupBy(s => s.Count()).ToDictionary(g => g.Key) is var d2
							? d1[3].First().Key.CompareTo(d2[3].First().Key) is int compbig && compbig != 0 ? compbig : d1[2].First().Key.CompareTo(d2[2].First().Key)
							: throw new InvalidOperationException("Failed to group when comparing full houses!")
		);

		public readonly static PokerHandType FourOfAKind = new(
				"Four of a kind",
				(IEnumerable<Card> cards) =>
					cards.GroupBy(c => c.Value)
					.Where(s => s.Count() == 4)
					.OrderByDescending(s => s.First().Value)
					.FirstOrDefault()
				,
				(c1, c2) => c1.HighestValue().CompareTo(c2.HighestValue())
		);

		public readonly static PokerHandType StraightFlush = new(
				name: "Straight Flush",
				isOfType: (IEnumerable<Card> cards) =>
					new Suit[4] { Suit.Hearts, Suit.Clubs, Suit.Spades, Suit.Diamonds }.Select(
						s =>
							Straight.IsOfType(cards.Where(c => c.Suit == s))
						).Where(s => s is not null).OrderByDescending(s => s!.First().Value)
					.FirstOrDefault()
				,
				comparator: (c1, c2) => Straight.Comparator(c1, c2)
		);

		public readonly static PokerHandType RoyalFlush = new(
				"Royal Flush",
				(IEnumerable<Card> cards) =>
					cards.Count() >= 5
					&& cards.Order().Take(5) is var relevantCards
					&& relevantCards.SameSuit()
					&& relevantCards.AreOfValues([CardValue.Ace, CardValue.King, CardValue.Queen, CardValue.Jack, CardValue.Ten]) ? relevantCards : null
				,
				(c1, c2) => 0
		);
		#endregion

		public readonly static PokerHandType[] TypesDescending = [RoyalFlush, StraightFlush, FourOfAKind, FullHouse, Flush, Straight, ThreeOfAKind, TwoPair, Pair, HighCard];
	}

	
}
