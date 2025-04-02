using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker
{
	public class PotInfo
	{
		public PlayerInfo[] PlayersInvolved;
		public bool IsLocked;
		public int Stake;
		public int TotalPot;
		public readonly int StakeOffset;
		public readonly int MaxPotStake;

		public PotInfo(PlayerInfo[] players, int stakeOffset)
		{
			PlayersInvolved = players;
			IsLocked = false;
			Stake = 0;
			TotalPot = 0;
			StakeOffset = stakeOffset;

			MaxPotStake = int.MaxValue;
			foreach (var p in PlayersInvolved)
			{
				if (p.Stack < MaxPotStake) MaxPotStake = p.Stack;
			}
		}

		public bool IsPlaying(PlayerInfo player)
		{
			return PlayersInvolved.Contains(player);
		}

		private int Level(PlayerInfo p)
		{
			int l = Stake + StakeOffset - p.CurrentStake;
			return l < 0 ? 0 : l;
		}

		public int GetMaxBet(PlayerInfo player)
		{
			int tallestStack = 0;
			foreach (var p in PlayersInvolved)
			{
				if (p.Name != player.Name && p.Stack - Level(p) > tallestStack) tallestStack = p.Stack - Level(p);
			}

			int absMax = tallestStack > player.Stack - Level(player) ? player.Stack : tallestStack;
			return Level(player) + absMax > player.Stack ? player.Stack - Level(player) : absMax;
		}

		public int GetStake(PlayerInfo player)
		{
			return player.CurrentStake - StakeOffset;
		}

		public PlayerInfo[] GetNextPotPlayers()
		{
			List<PlayerInfo> p = new();
			foreach (var pl in PlayersInvolved)
			{
				if (pl.Stack + pl.CurrentStake > MaxPotStake + StakeOffset && pl.Status != PlayerStatus.Folded) p.Add(pl);
			}
			return p.ToArray();
		}


	}
}
