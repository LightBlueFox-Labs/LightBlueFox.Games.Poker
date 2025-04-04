using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Evaluation;
using System.Diagnostics;
using System.Reflection;

namespace Tests.LightBlueFox.Games.Poker.Evaluation;

[TestClass]
public class FindStraightTests
{
    public static string StraightMethodDisplay(MethodInfo methodInfo, object[] data)
    {
        return string.Format("Straight Hand: {0} Table: {1}; Expected: {2}", data[0], data[1], data[2]);
    }

    public static IEnumerable<object[]> TestStraightHands => Helpers.AddIndexes([
            ["4H5H", "6H7H8H2S3S", true],
            ["4S4H", "5C6D7H2CKS", false],
            ["4S4H", "5C6D7H3CKS", true],
            ["AS2D", "3H5C2S4C5H", true],
            ["AS2D", "3H5C2S4C5H", true],
        ]);

    [TestMethod]
    [DynamicData(nameof(TestStraightHands), DynamicDataDisplayName = nameof(StraightMethodDisplay))]
    public void StraightTest(string hand, string table, bool expected, int index)
    {

        List<Card> cards = Helpers.FromString(table + hand);
        Debug.WriteLine("[StraightEvaluation] Testing Straight Hand " + index);
        var straight = PokerHandType.Evaluate(cards).Type.IsAnyStraight;
        Assert.IsTrue(straight == expected, "Straight with hand {0} and table {1} expected to eval to {2}!", hand, table, expected);
    }



}
