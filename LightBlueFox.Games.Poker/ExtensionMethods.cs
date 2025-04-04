using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker
{
    public static class ExtenionMethods
    {
        public static bool ScrambledEquals<T>(this IEnumerable<T> me, IEnumerable<T> other) where T: notnull
        {
			var cnt = new Dictionary<T, int>();
			foreach (T s in me)
			{
				if (cnt.ContainsKey(s))
				{
					cnt[s]++;
				}
				else
				{
					cnt.Add(s, 1);
				}
			}
			foreach (T s in other)
			{
				if (cnt.ContainsKey(s))
				{
					cnt[s]--;
				}
				else
				{
					return false;
				}
			}
			return cnt.Values.All(c => c == 0);
		}
        public static PlayerInfo[] ToInfo(this PlayerHandle[] players)
        {
            return Array.ConvertAll<PlayerHandle, PlayerInfo>(players, p => p);
        }

        public static string Info(this PokerAction action, int level = 0)
        {
            if (action == PokerAction.Call) return action + " (" + level + ")";
            else if (action == PokerAction.Raise) return action + " (" + (level != 0 ? level + " + " : "") + "AMOUNT)";
            else return action.ToString();
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
                if (pi >= 0)
                {
                    p.PlayersInvolved[pi] = player;
                }
            }
        }
    }
}
