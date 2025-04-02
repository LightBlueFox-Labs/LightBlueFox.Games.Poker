using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Utils;
using System.Diagnostics;
using System.Reflection;

namespace PokerTests;

[TestClass]
public class HandTests
{
	#region Hand Eval

	public static string HandMethodDisplay(MethodInfo methodInfo, object[] data)
	{
		return string.Format("Hand: {0} Table: {1}; Expected: {2}", data[0], data[1], data[2]);
	}

	public static IEnumerable<object[]> TestHands
	{
		get
		{
			return Helpers.AddIndexes(new[]
			{
				new object[]{"4H5H", "6H7H8H9HAH", PokerHands.StraightFlush, "5H6H7H8H9H" },
				new object[]{"2H3H", "6H7H8H2SAH", PokerHands.Flush, "3H6H7H8HAH" },
				new object[]{"AH4H", "5H6H7H8HQH", PokerHands.StraightFlush, "4H5H6H7H8H"},
				new object[]{"2H2S", "3D4H2DQCQD", PokerHands.FullHouse, "2H2S2DQCQD"},
				new object[]{"QD3H", "QSQHKSKH3D", PokerHands.FullHouse, "QDQSQHKSKH" },
				new object[]{"2C-5S", "TD-JS-QH-KC-AS", PokerHands.Straight, "TD-JS-QH-KC-AS"},
				new object[]{"2S-5C", "QS-KH-JH-TH-KC", PokerHands.Pair, "KH-KC"},
				new object[]{"9S-KS", "7D-7S-TC-KH-AH", PokerHands.TwoPair, "KS-KH-7D-7S"},
				new object[]{"AS-2D", "4C-6S-8D-JH-KD", PokerHands.HighCard, "AS"},
				new object[]{"JD-6C", "8D-6S-JC-3C-6H", PokerHands.FullHouse, "6C-6S-6H-JD-JC"},
				new object[]{"JD-6S", "2S-KC-JS-JC-JH", PokerHands.FourOfAKind, "JD-JS-JC-JH"},
				new object[]{"KD-6C", "2D-8D-9S-JS-QS", PokerHands.HighCard, "KD"},
				new object[]{"8D-9H", "8S-QC-8H-JS-AC", PokerHands.ThreeOfAKind, "8D-8S-8H"},
			});
		}
	}

	[TestMethod]
	[DynamicData(nameof(TestHands), DynamicDataDisplayName = nameof(HandMethodDisplay))]
	public void HandEvaluationTests(string hand, string tableCards, PokerHands handExpected, string expectedMaincards, int index)
	{
		Card[] _tableCards = Helpers.FromString(tableCards).ToArray();
		Card[] _hand = Helpers.FromString(hand).ToArray();

		Debug.WriteLine("[TotalHandEvaluationTests] Testing Hand " + index);

		Debug.WriteLine("[TotalHandEvaluationTests] ---> Expected: " + handExpected + "; Evaluated: " + HandEvaluation.GetEvaluation(_hand, _tableCards).HandType);
		Assert.IsTrue(HandEvaluation.GetEvaluation(_hand, _tableCards).HandType == handExpected, "Expected Hand Type " + handExpected + "; evaluated to " + HandEvaluation.GetEvaluation(_hand, _tableCards).HandType);

		Debug.WriteLine("[TotalHandEvaluationTests] ---> Expected: " + expectedMaincards + "; Evaluated: " + HandEvaluation.GetEvaluation(_hand, _tableCards).MainCards);
		Assert.IsTrue(HandEvaluation.ScrambledEquals(HandEvaluation.GetEvaluation(_hand, _tableCards).MainCards, Helpers.FromString(expectedMaincards))
			, "Expected main cards " + expectedMaincards + "; evaluated to " + HandEvaluation.GetEvaluation(_hand, _tableCards).MainCards);

	}
	#endregion

	#region Hand Comparison
	public static string ComparisonMethodDisplay(MethodInfo methodInfo, object[] data)
	{
		return string.Format("Hand: {0} vs {1}; Table: {2}; Expected: {3} ({4}|{5})", data[0], data[1], data[2], ((int)data[3] > 1 ? "win" : "loss"), data[4], data[5]);
	}

	public static IEnumerable<object[]> TestComparisons
	{
		get
		{
			return Helpers.AddIndexes(new[]
			{
				new object[]{"4H5H", "3H2D", "6H7H8H2S3S", 1, PokerHands.StraightFlush, PokerHands.TwoPair },
				new object[]{"QD3H", "KH5D", "QSQHKSKH3D", -1, PokerHands.FullHouse, PokerHands.FullHouse },
				new object[]{"5S3C", "5D4H", "5C4S3HQHJD", -1, PokerHands.TwoPair, PokerHands.TwoPair},
				new object[]{"AHQS", "QDQH", "ASADQC4C2S", 1, PokerHands.FullHouse, PokerHands.FullHouse},
				new object[]{"3S5H", "6H2S", "6D5C3C2CKH", -1, PokerHands.TwoPair, PokerHands.TwoPair},
				new object[]{"AH2S", "2D6H", "3S4D5HKDKH", -1, PokerHands.Straight, PokerHands.Straight},
				new object[]{"AH3D", "9D3S", "KSQDJCTS2D", 1, PokerHands.Straight, PokerHands.Straight },
				new object[]{"KSJD", "4S5D", "2H2D7C7D7S", 0, PokerHands.FullHouse, PokerHands.FullHouse},
				new object[] { "KS2D", "QSJD", "TSTD3H5C9C", 1, PokerHands.Pair, PokerHands.Pair},
			});
		}
	}

	[TestMethod]
	[DynamicData(nameof(TestComparisons), DynamicDataDisplayName = nameof(ComparisonMethodDisplay))]
	public void HandComparisonTests(string h1, string h2, string table, int expectedResult, PokerHands h_1, PokerHands h_2, int index)
	{
		Debug.WriteLine("[HandComparisonTests] Evaluating comparison " + index);

		Card[] tableCards = Helpers.FromString(table).ToArray();
		Card[] H1 = Helpers.FromString(h1).ToArray();
		Card[] H2 = Helpers.FromString(h2).ToArray();
		EvalResult e1 = HandEvaluation.GetEvaluation(H1, tableCards);
		EvalResult e2 = HandEvaluation.GetEvaluation(H2, tableCards);

		HandCompRes res = (HandCompRes)HandEvaluation.CompareHands(H1, H2, tableCards);
		Assert.IsTrue((int)res == expectedResult, "H1 ({0}) was expected to be " + ((HandCompRes)expectedResult) + " than/to H2 ({1}). Table: {2}, H1 Eval: {3}; H2 Eval: {4}. Result: {5}", h1, h2, table, e1.HandType, e2.HandType, res);

		Assert.IsTrue(h_1 == e1.HandType, "H1 ({0}) was expected to be {1}, but was evaluated to {2}", H1, h_1, e1.HandType);

		Assert.IsTrue(h_2 == e2.HandType, "H2 ({0}) was expected to be {1}, but was evaluated to {2}", H2, h_2, e1.HandType);
	}

	#endregion


}