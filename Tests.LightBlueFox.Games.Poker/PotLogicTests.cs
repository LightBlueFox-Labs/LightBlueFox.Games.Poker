using LightBlueFox.Games.Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Games.Poker
{
	[TestClass]
	public class PotLogicTests
	{
		private static PlayerInfo[] dummyPlayers =
		{
			new("Jay", 500),
			new("Steve", 1000),
			new("Seth", 1500),

		};

		private static PlayerInfo[] players = {};

		private void cleanupPlayers()
		{
			players = dummyPlayers;
		}

		private void setPlayerStakes(params int[] stakes)
		{
			if (stakes.Length != players.Length) throw new Exception();
			for (int i = 0; i < stakes.Length; i++)
			{
				players[i].CurrentStake = stakes[i];
			}
		}

		public PotInfo[] getBasicPots()
		{
			return new[]
			{
				new PotInfo(players, 0)
			};
		}

		[TestMethod]
		public void SimpleMaxBetTests()
		{
			cleanupPlayers();
			setPlayerStakes(100, 100, 900);
			PotInfo p = getBasicPots()[0];
			p.Stake = 900;
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[0]) == -300);
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[1]) == 200);
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[2]) == 200);

			cleanupPlayers();
			setPlayerStakes(100, 800, 100);
			p = getBasicPots()[0];
			p.Stake = 800;
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[2]) == 800);
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[0]) == -200);
			Assert.IsTrue(p.GetMaxBet(dummyPlayers[1]) == 800);
			

		}
	}
}
