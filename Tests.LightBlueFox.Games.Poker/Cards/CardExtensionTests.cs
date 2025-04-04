using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Evaluation;
using System.Diagnostics;

namespace Tests.LightBlueFox.Games.Poker.Cards
{
	[TestClass]
	public class CardExtensionTests
	{

		public static IEnumerable<object[]> SequenceTestData => 
			[
				["4H5H6H7H8H2S3S", "8765432"],
				["4S4H5C6D7H2CKS", "7654"],
				["4S4H5C6D7H3CKS", "76543"],
				["AS2D3H5C2S4C5H", "5432"],
			];


		[TestMethod]
		[DynamicData(nameof(SequenceTestData))]
		public void FindLongestSequence(string c, string expectedSeq)
		{

			List<Card> cards = Helpers.FromString(c);
			var seq = cards.GetLongestSequence()
				.Select(c => c.ToString()![..1])
				.Aggregate((s, t) => s + t);
			Assert.IsTrue(seq == expectedSeq, "Longest sequence with cards {0} expected to get {1}!", c, expectedSeq);
		}

	}
}
