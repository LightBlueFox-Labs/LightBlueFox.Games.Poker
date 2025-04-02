using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker.Web
{
	public static class GameManager
	{
		public static Dictionary<string, Game> runningGamesByID = new();

		public static void JoinGame(string gameID, PlayerHandle player)
		{
			if (gameID == null) throw new ArgumentNullException("Game id is null!");
			if (!runningGamesByID.ContainsKey(gameID))
			{
				runningGamesByID.Add(gameID, new Game(gameID));
			}

			var game = runningGamesByID[gameID];
			game.AddPlayer(player);
		}

		public static void TryStartRound(string gameID)
		{
			if (CanStart(gameID)) Task.Run(() => { runningGamesByID[gameID].StartRound(); });
		}

		public static bool CanStart(string gameID)
		{
			var res = runningGamesByID.ContainsKey(gameID) && runningGamesByID[gameID].State != GameState.InRound && runningGamesByID[gameID].Players.Count > 1;
			return res;
		}

		public static void DisconnectUser(string gameID, PlayerHandle player)
		{
			runningGamesByID[gameID].RemovePlayer(player);
		}

		public static string getAnyRunningGame()
		{
			if (runningGamesByID.Count == 0) return "";
			else return runningGamesByID.Values.OrderByDescending((g) => g.Players.Count).First().ID;
		}
	}
}
