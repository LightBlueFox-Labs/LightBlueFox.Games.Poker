using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Evaluation;
using LightBlueFox.Games.Poker.Exceptions;
using LightBlueFox.Games.Poker.Player;
using LightBlueFox.Games.Poker.Web.Pages;
using System.Linq;

namespace LightBlueFox.Games.Poker.Web.Controllers
{
    public class GameController : PlayerHandle
	{
		public GameView View { get; private set; }

		public bool CanStartRound
		{
			get
			{
				return !View.IsRoundRunning && Player.Stack >= View.GameInfo!.BigBlind && View.OtherPlayers.Any((p) =>
				{
					return p.IsConnected && p.Stack >= View.GameInfo.BigBlind;
				});
			}
		}

		public void StartGame()
		{
			GameManager.TryStartRound(View.GameID);
		}

		public GameController(GameView gameView) : base(gameView.PlayerName)
		{
			View = gameView;
			GameManager.JoinGame(gameView.GameID, this);
			View.IsLoading = false;
		}

		public override void OtherPlayerDoes(PlayerInfo playerInfo, ActionInfo action)
		{
			View.Log("[{0}]: Player {1} performed {2}.", this.Player.Name, playerInfo.Name, action.ActionType);
			UpdatePlayerInfo(playerInfo);

			//throw new NotImplementedException();
		}

		public override void PlayerConnected(PlayerInfo playerInfo, bool wasReconnect)
		{
			View.Log("[{0}]: Player {1} " + (wasReconnect ? "reconnected." : "connected."), this.Player.Name, playerInfo.Name);
			if (playerInfo.Name == this.Player.Name)
			{
				View.MyIndex = View.OtherPlayers.Length;
				UpdatePlayerInfo(playerInfo);
			}
			else
			{
				if (!View.OtherPlayers.Any((p) => p.Name == playerInfo.Name)) View.OtherPlayers = View.OtherPlayers.Append(playerInfo).ToArray();
			}

			View.Rerender();
		}

		public override void PlayerDisconnected(PlayerInfo playerInfo)
		{
			View.Log("[{0}]: Player {1} disconnected.", this.Player.Name, playerInfo.Name);
			if (View.IsRoundRunning)
			{
				UpdatePlayerInfo(playerInfo, true);
			}
			else
			{
				View.RemovePlayers((p) => p.Name == playerInfo.Name);
			}
			View.Rerender();
		}

		public override void ChangePlayer(PlayerInfo player)
		{
			base.ChangePlayer(player);
			View.MyPlayer = player;
			View.Rerender();
		}

		public override void Reconnected(PlayerInfo yourPlayer, Card[]? yourCards, GameInfo gameInfo, PlayerInfo[] otherPlayers, Card[]? tableCards, PotInfo[]? pots, int currentMinBet)
		{
			View.MyPlayer = yourPlayer;
			View.MyCards = yourCards;
			View.GameInfo = gameInfo;
			View.TableCards = tableCards;
			View.Pots = pots;
			View.MinBet = currentMinBet;
			View.IsRoundRunning = gameInfo.GameState == GameState.InRound;
			View.Rerender();
		}

		public override void StartSpectating(PlayerInfo yourPlayer, GameInfo gameInfo, PlayerInfo[] otherPlayers, Card[]? tableCards, PotInfo[]? pots, int currentMinBet)
		{
			View.MyPlayer = yourPlayer;
			View.MyCards = null;
			View.GameInfo = gameInfo;
			View.TableCards = tableCards;
			View.Pots = pots;
			View.MinBet = currentMinBet;
			View.IsRoundRunning = gameInfo.GameState == GameState.InRound;
			View.Rerender();
		}

		protected override void NewDealRound(Card[] cards, int minBet)
		{
			View.Log("[{0}]: Table cards changed, are now {1}.", Player.Name, PrintCardCollection(cards));
			View.TableCards = cards;
			View.MinBet = minBet;
			View.Rerender();
		}

		public override void TellGameInfo(GameInfo gameInfo)
		{
			View.Log($"Received Game info: Game ID {gameInfo.ID}, SB {gameInfo.SmallBlind}, BB {gameInfo.BigBlind}, currentState {gameInfo.GameState}");

			View.GameInfo = gameInfo;
		}

		protected override ActionInfo DoTurn(PokerAction[] actions)
		{
			View.Log("\n[{0}]: Your turn. You can perform the following actions: ", Player.Name);
			for (int i = 0; i < actions.Length; i++)
			{
				if (CurrentPots != null) View.Log(" [{0}] {1}", i, actions[i].Info(CurrentPots.Last().Stake + CurrentPots.Last().StakeOffset - Player.CurrentStake));
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
			UpdatePlayerInfo(player);
		}

		private void UpdatePlayerInfo(PlayerInfo p, bool suppress = false)
		{
			if (p.Name == Player.Name) ChangePlayer(p);
			else View?.UpdatePlayerInfo(p, suppress);
		}

		protected override void RoundEnded(RoundResult res)
		{
			View.WhoseTurn = null;
			foreach (var item in res.Summaries)
			{
				UpdatePlayerInfo(item.Player, true);
			}
			View.roundEnd = res;
			foreach (var pot in res.PotResults)
			{
				View.Log("[{0}]: (Side)pot: {1}", Player.Name, pot.Pot.TotalPot);
				foreach (var i in pot.PlayerInfos)
				{
					View.Log($"     [{i.Player.Name}{(i.Player.Name == Player.Name ? " (YOU)" : "")}] {(i.HasWon ? "won (+" + (i.ReceivedCoins - i.Player.CurrentStake) : "lost (-" + i.Player.CurrentStake)} coins). {(i.Cards is not null ? "Cards: " + PrintCardCollection(i.Cards) : "")}{(i.Evaluation is not null ? ", eval: " + i.Evaluation : "")}");
				}

			}
			View.Log("------------- ROUND END -----------\n\n");
			View.Rerender();
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

		public override void PlayersTurn(PlayerInfo player, bool isNewRound)
		{
			UpdatePlayerInfo(player);
			View.Log($"It's {player.Name}'s turn.");
			View.WhoseTurn = player;
			View.Rerender();
			View.StartTurnTimer(player, Round.TURN_TIMEOUT);
		}

		private bool isDisconnected = false;
		public void Disconnect()
		{
			if (!isDisconnected)
			{
				isDisconnected = true;
				GameManager.DisconnectUser(View.GameID, this);
			}
		}

		public override void TurnCanceled(PlayerInfo player, TurnCancelReason reason)
		{
			if (player.Name == this.Player.Name)
			{
				View.AbortTurn();
			}
		}

		public override void InformException(SerializedExceptionInfo exception)
		{
			switch (exception.Consequence)
			{
				case ExceptionConsequence.Kicked:
				case ExceptionConsequence.GameClose:
					View.DisplayMainScreenError(exception.Message);
					break;
				default:
					View.DisplayExceptionBanner(exception.Message);
					break;
			}
		}

		public override void RoundClosed()
		{
			View.IsRoundRunning = false;
			View.TableCards = null;
			View.roundEnd = null;
			View.WhoseTurn = null;
			View.AbortTurn();
			View.Rerender();
		}

		public override void GameClosed()
		{
			View.DisplayMainScreenError("Game closed.");
		}
	}
}
