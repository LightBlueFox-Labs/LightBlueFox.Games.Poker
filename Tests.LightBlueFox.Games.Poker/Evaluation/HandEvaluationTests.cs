using LightBlueFox.Games.Poker;
using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Evaluation;
using System.Diagnostics;
using System.Reflection;

namespace Tests.LightBlueFox.Games.Poker.Evaluation;

[TestClass]
public class HandTests
{
    #region Hand Eval

    public static string HandMethodDisplay(MethodInfo methodInfo, object[] data)
    {
        return string.Format("Hand: {0} Table: {1}; Expected: {2}", data[0], data[1], data[2]);
    }

    public static IEnumerable<object[]> TestHands => Helpers.AddIndexes([
                ["4H5H", "6H7H8H9HAH", PokerHandType.StraightFlush, "5H6H7H8H9H"],
                ["2H3H", "6H7H8H2SAH", PokerHandType.Flush, "3H6H7H8HAH"],
                ["AH4H", "5H6H7H8HQH", PokerHandType.StraightFlush, "4H5H6H7H8H"],
                ["2H2S", "3D4H2DQCQD", PokerHandType.FullHouse, "2H2S2DQCQD"],
                ["QD3H", "QSQHKSKH3D", PokerHandType.FullHouse, "QDQSQHKSKH"],
                ["AH4S", "6D3C2S5HKD", PokerHandType.Straight, "6D5H4S3C2S"],
                ["2C-5S", "TD-JS-QH-KC-AS", PokerHandType.Straight, "TD-JS-QH-KC-AS"],
                ["2S-5C", "QS-KH-JH-TH-KC", PokerHandType.Pair, "KH-KC"],
                ["9S-KS", "7D-7S-TC-KH-AH", PokerHandType.TwoPair, "KS-KH-7D-7S"],
                ["AS-2D", "4C-6S-8D-JH-KD", PokerHandType.HighCard, "AS"],
                ["JD-6C", "8D-6S-JC-3C-6H", PokerHandType.FullHouse, "6C-6S-6H-JD-JC"],
                ["JD-6S", "2S-KC-JS-JC-JH", PokerHandType.FourOfAKind, "JD-JS-JC-JH"],
                ["KD-6C", "2D-8D-9S-JS-QS", PokerHandType.HighCard, "KD"],
                ["8D-9H", "8S-QC-8H-JS-AC", PokerHandType.ThreeOfAKind, "8D-8S-8H"],
            ]);

    [TestMethod]
    [DynamicData(nameof(TestHands))]
    public void HandEvaluationTests(string hand, string tableCards, PokerHandType handExpected, string expectedMaincards, int index)
    {
        Card[] _tableCards = Helpers.FromString(tableCards).ToArray();
        Card[] _hand = Helpers.FromString(hand).ToArray();

        Debug.WriteLine("[TotalHandEvaluationTests] Testing Hand " + index);
        var eval = HandEvaluation.Evaluate(_tableCards, _hand);

		Debug.WriteLine("[TotalHandEvaluationTests] ---> Expected: " + handExpected + "; Evaluated: " + eval.Type);
        Assert.IsTrue(eval.Type == handExpected, "Expected Hand Type " + handExpected + "; evaluated to " + eval.Type);

        Debug.WriteLine("[TotalHandEvaluationTests] ---> Expected: " + expectedMaincards + "; Evaluated: " + eval.Hand.Print("-"));
        Assert.IsTrue(eval.Hand.ScrambledEquals(Helpers.FromString(expectedMaincards))
            , "Expected main cards " + expectedMaincards + "; evaluated to " + eval.Hand.Print("-"));

    }
    #endregion

    #region Hand Comparison
    public static string ComparisonMethodDisplay(MethodInfo methodInfo, object[] data)
    {
        return string.Format("Hand: {0} vs {1}; Table: {2}; Expected: {3} ({4}|{5})", data[0], data[1], data[2], (int)data[3] > 1 ? "win" : "loss", data[4], data[5]);
    }

    public static IEnumerable<object[]> TestComparisons
    {
        get
        {
            return Helpers.AddIndexes(
            [
                ["4H5H", "3H2D", "6H7H8H2S3S", 1, PokerHandType.StraightFlush, PokerHandType.TwoPair],
                ["QD3H", "KH5D", "QSQHKSKH3D", -1, PokerHandType.FullHouse, PokerHandType.FullHouse],
                ["5S3C", "5D4H", "5C4S3HQHJD", -1, PokerHandType.TwoPair, PokerHandType.TwoPair],
                ["AHQS", "QDQH", "ASADQC4C2S", 1, PokerHandType.FullHouse, PokerHandType.FullHouse],
                ["3S5H", "6H2S", "6D5C3C2CKH", -1, PokerHandType.TwoPair, PokerHandType.TwoPair],
                ["AH2S", "2D6H", "3S4D5HKDKH", -1, PokerHandType.Straight, PokerHandType.Straight],
                ["AH3D", "9D3S", "KSQDJCTS2D", 1, PokerHandType.Straight, PokerHandType.Straight],
                ["KSJD", "4S5D", "2H2D7C7D7S", 0, PokerHandType.FullHouse, PokerHandType.FullHouse],
                ["KS2D", "QSJD", "TSTD3H5C9C", 1, PokerHandType.Pair, PokerHandType.Pair],
            ]);
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestComparisons), DynamicDataDisplayName = nameof(ComparisonMethodDisplay))]
    public void HandComparisonTests(string h1, string h2, string table, int expectedResult, PokerHandType h_1, PokerHandType h_2, int index)
    {
        Debug.WriteLine("[HandComparisonTests] Evaluating comparison " + index);

        Card[] tableCards = Helpers.FromString(table).ToArray();
        Card[] H1 = Helpers.FromString(h1).ToArray();
        Card[] H2 = Helpers.FromString(h2).ToArray();
        HandEvaluation e1 = HandEvaluation.Evaluate(tableCards, H1);
        HandEvaluation e2 = HandEvaluation.Evaluate(tableCards, H2);
        Dictionary<int, string> names = new() { { -1, "weaker" }, { 0, "equal" }, { 1, "stronger" } };
        int res = e1.CompareTo(e2);
        Assert.IsTrue(res == expectedResult, "H1 ({0}) was expected to be " + names[expectedResult] + " than/to H2 ({1}). Table: {2}, H1 Eval: {3}; H2 Eval: {4}. Result: {5}", h1, h2, table, e1.Type, e2.Type, res);

        Assert.IsTrue(h_1 == e1.Type, "H1 ({0}) was expected to be {1}, but was evaluated to {2}", H1, h_1, e1.Type);

        Assert.IsTrue(h_2 == e2.Type, "H2 ({0}) was expected to be {1}, but was evaluated to {2}", H2, h_2, e1.Type);
    }

    #endregion


}