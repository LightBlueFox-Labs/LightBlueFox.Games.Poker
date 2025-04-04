namespace LightBlueFox.Games.Poker.Cards
{
	public struct Card : IEquatable<Card>, IComparable<Card>
	{
		public readonly CardValue Value;
		public readonly Suit Suit;

		public Card(Suit s, CardValue v)
		{
			Value = v; Suit = s;
		}

		private readonly static Dictionary<char, CardValue> ValueChars = new()
		{
			{'2', CardValue.Two },
			{'3', CardValue.Three },
			{'4', CardValue.Four },
			{'5', CardValue.Five },
			{'6', CardValue.Six },
			{'7', CardValue.Seven },
			{'8', CardValue.Eight },
			{'9', CardValue.Nine },
			{'T', CardValue.Ten },
			{'J', CardValue.Jack },
			{'Q', CardValue.Queen },
			{'K', CardValue.King },
			{'A', CardValue.Ace },
		};

		private static IReadOnlyDictionary<CardValue, char> CharsByValue => ValueChars.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		private readonly static Dictionary<char, Suit> SuitChars = new()
		{
			{'H', Suit.Hearts },
			{'D', Suit.Diamonds },
			{'C', Suit.Clubs },
			{'S', Suit.Spades },
		};

		private static IReadOnlyDictionary<Suit, char> CharsBySuit => SuitChars.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		public Card(string id)
		{
			if (id == null || id.Length != 2) throw new ArgumentException("String for card needs to be length 2: One char for the value, one for the suit. Example: 4H = Four of Hearts.");
			char[] chars = id.ToCharArray();

			if (!SuitChars.ContainsKey(chars[1])) throw new ArgumentException("No suit known with " + chars[1] + "!");
			if (!ValueChars.ContainsKey(chars[0])) throw new ArgumentException("No card value known with " + chars[0] + "!");

			Value = ValueChars[chars[0]];
			Suit = SuitChars[chars[1]];
		}

		public bool Equals(Card other)
		{
			return other.Suit == Suit && other.Value == Value;
		}

		public override bool Equals(object? obj)
		{
			return obj != null && obj is Card c && Equals(c);
		}

		public static bool operator ==(Card left, Card right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Card left, Card right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Value.GetHashCode(), Suit.GetHashCode());
		}

		public int CompareTo(Card other)
		{
			return Value.CompareTo(other.Value);
		}
		public override string ToString()
		{
			return "" + CharsByValue[Value] + CharsBySuit[Suit];
		}
	}
	public enum CardValue
	{
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13,
		Ace = 14
	}
	public enum Suit
	{
		Spades,
		Hearts,
		Clubs,
		Diamonds
	}


}
