using LightBlueFox.Games.Poker.Web.Pages;
using LightBlueFox.Games.Poker;
using LightBlueFox.Games.Poker.PlayerHandles.Remote;
using LightBlueFox.Games.Poker.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LightBlueFox.Games.Poker.Web.Controllers
{
	public class GameController : PlayerHandle
	{
		public readonly GameView View;

		public void StartGame()
		{
			View.CanStartRound = false;
			GameManager.TryStartRound(View.GameID);
		}

		public GameController(GameView gameView) : base(gameView.PlayerName) {
			View = gameView;
			GameManager.JoinGame(gameView.GameID, this);
			View.IsLoading = false;
		}

		public override void OtherPlayerDoes(PlayerInfo playerInfo, ActionInfo action)
		{
			View.Log("[{0}]: Player {1} performed {2}.", this.Player.Name, playerInfo.Name, action.ActionType);
			View.UpdatePlayerInfo(playerInfo);
			
			//throw new NotImplementedException();
		}

		public override void PlayerConnected(PlayerInfo playerInfo, bool wasReconnect)
		{
			View.Log("[{0}]: Player {1} " + (wasReconnect ? "reconnected." : "connected."), this.Player.Name, playerInfo.Name);
			if (playerInfo.Name == this.Player.Name)
			{
				this.ChangePlayer(playerInfo);
			}
			else
			{
				View.OtherPlayers = View.OtherPlayers.Append(playerInfo).ToArray();
			}

			if (View.OtherPlayers.Length > 0) View.CanStartRound = true;
			View.Rerender();
		}

		public override void PlayerDisconnected(PlayerInfo playerInfo)
		{
			View.Log("[{0}]: Player {1} disconnected.", this.Player.Name, playerInfo.Name);
			//throw new NotImplementedException();
		}
		
		public override void ChangePlayer(PlayerInfo player)
		{
			base.ChangePlayer(player);
			View.MyPlayer = player;
			View.Rerender();
		}

		public override void Reconnect(PlayerHandle newPlayerHandle)
		{
			//throw new NotImplementedException();
		}

		protected override void NewDealRound(Card[] cards, int minBet)
		{
			View.Log("[{0}]: Table cards changed, are now {1}.", Player.Name, PrintCardCollection(cards));
			View.TableCards = cards;
			View.MinBet = minBet;
			View.Rerender();
		}

		public override void TellGameInfo(PokerProtocol.GameInfo gameInfo)
		{
			View.Log($"Received Game info: Game ID {gameInfo.ID}, SB {gameInfo.SmallBlind}, BB {gameInfo.BigBlind}, currentState {gameInfo.GameState}");
			
			View.GameInfo = gameInfo;
		}

		protected override ActionInfo DoTurn(PokerAction[] actions)
		{
			View.Log("\n[{0}]: Your turn. You can perform the following actions: ", Player.Name);
			for (int i = 0; i < actions.Length; i++)
			{
				View.Log(" [{0}] {1}", i, actions[i].Info(CurrentGameStake - Player.CurrentStake));
			}

			var res = View.DoTurn(actions, MaxBet);
			View.Log($"You chose: {res.ActionType} (bet: {res.BetAmount}");
			return res;
		}

		protected override void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int totalStake, PotInfo[] pots)
		{
			View.Log($"[{Player}]: {player} bet {amount} {(wasBlind ? "(blind)" : "")}. Current Stakes: {totalStake}. Current Pot {0}. New MinBet: {newMinBet}");
			View.Pots = pots;
			View.MinBet = newMinBet;
			View.UpdatePlayerInfo(player);
		}

		protected override void RoundEnded(RoundResult res)
		{
			foreach (var item in res.Summaries)
			{
				View.UpdatePlayerInfo(item.Player, true);
			}
			View.roundEnd = res;
			foreach (var pot in res.PotResults)
			{
				View.Log("[{0}]: (Side)pot: {1}", Player.Name, pot.Pot.TotalPot);
				foreach (var i in pot.PlayerInfos)
				{
					View.Log($"     [{i.Player.Name}{(i.Player.Name == Player.Name ? " (YOU)" : "")}] {(i.HasWon ? "won (+" + (i.ReceivedCoins - i.Player.CurrentStake) : "lost (-" + i.Player.CurrentStake)} coins). {(i.CardsVisible ? "Cards: " + PrintCardCollection(i.Cards) : "")}{(i.Eval.Length == 1 ? ", eval: " + i.Eval[0] : "")}");
				}

			}
			View.Log("------------- ROUND END -----------\n\n");
		}

		protected override void RoundStarted(Card[] cards, PlayerInfo[] info, int RoundNR, int btn, int sb, int bb)
		{
			View.roundEnd = null;
			View.Log("------------ ROUND START ----------");
			View.Log("[{0}]: Round started. Your cards: {1}", Player.Name, PrintCardCollection(cards));

			View.IsRoundRunning = true;
			View.OtherPlayers = info.Where((p) =>
			{
				return p.Name != this.Player.Name;
			}).ToArray();

			View.Pots = CurrentPots;

			View.MyCards = cards;
			View.Rerender();
		}

		private static string[] SUITS = { "S", "H", "C", "D" };
		private static string[] VALS = { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };

		private static string PrintCardCollection(Card[] cards)
		{
			List<string> strs = new List<string>();
			foreach (var card in cards)
			{
				strs.Add(VALS[(int)card.Value - 2] + SUITS[(int)card.Suit]);
			}
			return string.Join("-", strs);
		}

		public override void PlayersTurn(PokerProtocol.PlayersTurn pt)
		{
			View.UpdatePlayerInfo(pt.Player);
			View.Log($"It's {pt.Player.Name}'s turn.");
			View.WhoseTurn = pt.Player;
			View.Rerender();
		}
	}
}
