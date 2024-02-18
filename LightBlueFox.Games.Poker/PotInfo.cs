using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
	[CompositeSerialize]
	public class PotInfo
	{
		public PlayerInfo[] PlayersInvolved;
		public bool IsLocked;
		public int Stake;
		public int TotalPot;
		public readonly int StakeOffset;
		public readonly int MaxPotStake;
		public PotInfo(PlayerInfo[] players, int stakeOffset) {
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

		private int level(PlayerInfo p)
		{
			int l =  Stake + StakeOffset - p.CurrentStake;
			return l < 0 ? 0 : l;
		}

		public int GetMaxBet(PlayerInfo player)
		{
			int tallestStack = 0;
			foreach (var p in PlayersInvolved)
			{
				if(p.Name != player.Name && p.Stack - level(p) > tallestStack) tallestStack = p.Stack - level(p);
			}

			int absMax = tallestStack > player.Stack - level(player) ? player.Stack : tallestStack;
			return level(player) + absMax > player.Stack ? player.Stack - level(player) : absMax;
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
				if(pl.Stack + pl.CurrentStake > MaxPotStake + StakeOffset && pl.Status != PlayerStatus.Folded) p.Add(pl);
			}
			return p.ToArray();
		}

		
	}
}
