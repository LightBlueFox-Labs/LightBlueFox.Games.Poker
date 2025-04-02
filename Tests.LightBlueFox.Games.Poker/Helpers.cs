using LightBlueFox.Games.Poker.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTests
{
	internal static class Helpers
	{
		public static object[][] AddIndexes(object[][] arr)
		{
			object[][] newArr = new object[arr.Length][];
			for (int i = 0; i < arr.Length; i++)
			{
				newArr[i] = new object[arr[i].Length + 1];
				Array.Copy(arr[i], newArr[i], arr[i].Length);
				newArr[i][^1] = i;
			}
			return newArr;
		}

		public static List<Card> FromString(string cards)
		{
			List<Card> res = new();
			cards = cards.Trim().ToUpper().Replace(" ", "").Replace("-", "");
			if (!string.IsNullOrEmpty(cards))
			{
				if (cards.Length % 2 != 0) throw new ArgumentException("String needs to be of even length!");
				for (int i = 0; i < cards.Length / 2; i++)
				{
					res.Add(new Card(cards.Substring(i * 2, 2)));
				}
			}
			return res;
		}
	}
}
