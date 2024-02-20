using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Utils
{
	public static class ExtenionMethods
	{
		public static bool IsStraight(this PokerHands hand)
		{
			return (new PokerHands[] { PokerHands.Straight, PokerHands.StraightFlush }).Contains(hand);
		}

		public static PlayerInfo[] ToInfo(this PlayerHandle[] players)
		{
			return Array.ConvertAll<PlayerHandle, PlayerInfo>(players, p => p);
		}

		public static PlayerInfo[] ToInfo(this List<PlayerHandle> players)
		{
			return Array.ConvertAll<PlayerHandle, PlayerInfo>(players.ToArray(), p => p);
		}

		public static PlayerInfo[] ToInfo(this IReadOnlyList<PlayerHandle> players)
		{
			return Array.ConvertAll<PlayerHandle, PlayerInfo>(players.ToArray(), p => p);
		}

		public static PotInfo GetRelevantPot(this PotInfo[] pots, PlayerInfo player)
		{
			for (int i = pots.Length - 1; i >= 0; i--)
			{
				if (pots[i].IsPlaying(player)) return pots[i];
			}
			throw new ArgumentException("The given player is not participating in any pot.");
		}

		public static int GetTotalPot(this PotInfo[] pots)
		{
			int total = 0; 
			foreach (var p in pots)
			{
				total += p.TotalPot;
			}
			return total;
		}

		public static void UpdatePlayers(this List<PotInfo> potInfos, PlayerInfo player)
		{
			foreach (var p in potInfos)
			{
				var pi = Array.IndexOf(p.PlayersInvolved, player);
				if(pi >= 0)
				{
					p.PlayersInvolved[pi] = player;
				}
			}
		}
	}
}
