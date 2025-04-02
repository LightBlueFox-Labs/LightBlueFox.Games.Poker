using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Exceptions;
using LightBlueFox.Games.Poker.Player;
using LightBlueFox.Games.Poker.Utils;

namespace LightBlueFox.Games.Poker
{
	public class Round
	{
		public const int TURN_TIMEOUT = 250000;
		private readonly static int[] _DEALS = { 0, 3, 1, 1 };


		public PlayerHandle[] Players;
		public readonly List<PlayerHandle> Spectators = [];
		public readonly List<Card> TableCards = [];
		public readonly DeckGenerator Deck = new();
		private readonly List<PotInfo> Pots = [];

		private PotInfo ActivePot => Pots.Last();

		private int _minBet = 0;
		private int RemainingPlayerCount;

		private readonly int ButtonIndex;
		private readonly int SBIndex;
		private readonly int BBIndex;
		private readonly int BB;
		private readonly int SB;

		public int CurrentMinBet => _minBet;


		private readonly int RoundNR;
		private readonly Game Game;

		public Round(PlayerHandle[] players, int sb, int bb, int buttonInd, int roundNR, Game game)
		{
			Game = game;
			Players = players;
			RemainingPlayerCount = players.Length;
			ButtonIndex = buttonInd;
			SBIndex = Players.Length > 2 ? (ButtonIndex + 1) % Players.Length : ButtonIndex; // For Heads-Up, Button and SB coincide.
			BBIndex = (SBIndex + 1) % Players.Length;
			BB = bb;
			SB = sb;

			if (ButtonIndex == SBIndex)
			{
				players[ButtonIndex].Role = PlayerRole.SmallBlind | PlayerRole.Button;
			}
			else
			{
				players[ButtonIndex].Role = PlayerRole.Button;
				players[SBIndex].Role = PlayerRole.SmallBlind;
			}
			players[BBIndex].Role = PlayerRole.BigBlind;


			RoundNR = roundNR;
			Pots.Add(new(Players.ToInfo(), 0));
		}

		private RoundResult InformResult(RoundResult res, ref GameState state)
		{
			state = GameState.NotRunning;
			Inform((p) => p.EndRound(res));
			return res;
		}

		private void Inform(Action<PlayerHandle> action, Action<PlayerHandle>? spectatorAction = null, bool suppressSpectators = false)
		{
			foreach (var p in Players)
			{
				if (p.IsConnected) action(p);
			}
			spectatorAction ??= action;
			if (!suppressSpectators)
			{
				foreach (var p in Spectators)
				{
					if (p.IsConnected) spectatorAction(p);
				}
			}
		}

		public RoundResult PlayRound(ref GameState state)
		{
			Inform((p) => p.StartRound(Deck.PopRandom(2), Players.ToInfo(), RoundNR, ButtonIndex, SBIndex, BBIndex), null, false);

			// SB Bet
			if (!TryBet(Players[SBIndex], SB, true, false)) throw new NotImplementedException("Player cannot provide blind!");
			// BB Bet
			if (!TryBet(Players[BBIndex], BB, true, false)) throw new NotImplementedException("Player cannot provide blind!");

			int roundOffset = (BBIndex + 1) % Players.Length;

			foreach (var d in _DEALS)
			{
				_minBet = BB;
				TableCards.AddRange(Deck.PopRandom(d));
				Game.Log($"[SERVER] New deal. Now {TableCards.Count}/5 Cards in the middle.");

				Inform((p) => p.NewCardsDealt([.. TableCards], _minBet));

				RemainingPlayerCount = Players.Count((p) => p.Status != PlayerStatus.Folded);

				int dealEnd = Players.Length;

				for (int i = 0; i < dealEnd; i++)
				{
					if (PerformPlayerTurn(Players[(i + roundOffset) % Players.Length], ref dealEnd, i, i == 0))
						return InformResult(RoundResult.DetermineRoundResult([.. TableCards], Players, [.. Pots]), ref state);
				}

				roundOffset = ButtonIndex + 1;
			}

			return InformResult(RoundResult.DetermineRoundResult([.. TableCards], Players, [.. Pots]), ref state);
		}

		public static bool CanBet(PlayerHandle player, int betAmount, int currentStake, int minBet, PokerAction? performedAction = null)
		{
			int level = currentStake - player.CurrentStake ?? throw new NullReferenceException();
			int absoluteBetSize = betAmount - level;
			return player.Stack >= betAmount
				&& (
					absoluteBetSize == 0 && (performedAction == PokerAction.Call || performedAction == null)
					||
					absoluteBetSize >= minBet && (performedAction == PokerAction.Raise || performedAction == null)
				);
		}

		private void PlacePotBetRecursively(int betAmount, PlayerInfo player)
		{
			if (player.CurrentStake > ActivePot.MaxPotStake + ActivePot.StakeOffset)
			{
				int betPcs = betAmount - (player.CurrentStake - (ActivePot.MaxPotStake + ActivePot.StakeOffset));
				ActivePot.TotalPot += betPcs;
				ActivePot.Stake = ActivePot.MaxPotStake;

				Pots.Add(new(ActivePot.GetNextPotPlayers(), ActivePot.StakeOffset + ActivePot.MaxPotStake));

				PlacePotBetRecursively(betAmount - betPcs, player);
			}
			else
			{
				ActivePot.TotalPot += betAmount;
				ActivePot.Stake = player.CurrentStake - ActivePot.StakeOffset;
			}
		}

		private bool TryBet(PlayerHandle player, int betAmount, bool isBlind, bool wasForcedAllIn, PokerAction? performedAction = null)
		{
			if (wasForcedAllIn && performedAction != PokerAction.Call) return false;
			/// HTIS IS SO UGLY PLS FIXXXXXXXXX
			if (!CanBet(player, betAmount, wasForcedAllIn ? player.Stack + player.Player.CurrentStake : ActivePot.Stake + ActivePot.StakeOffset, Math.Min(_minBet, ActivePot.GetMaxBet(player)), performedAction)) return false;

			var newP = player.Player;
			int oldPlayerStake = newP.CurrentStake;
			newP.CurrentStake += betAmount;
			newP.Stack -= betAmount;
			Pots.UpdatePlayers(player.Player);
			player.ChangePlayer(newP);

			if (wasForcedAllIn)
			{
				int potIndex = Pots.Count - 1;
				while (!Pots[potIndex].IsPlaying(player)) { potIndex -= 1; }
				Pots[potIndex].TotalPot += betAmount;
			}
			else
			{
				int level = ActivePot.Stake + ActivePot.StakeOffset - oldPlayerStake;
				int absoluteBetSize = betAmount - level;

				PlacePotBetRecursively(betAmount, newP);
				if (absoluteBetSize > _minBet) _minBet = absoluteBetSize;
			}

			Inform((p) => p.PlayerBet(player, betAmount, isBlind, _minBet, ActivePot.Stake + ActivePot.StakeOffset, Pots.ToArray()));


			return true;
		}

		public PlayerInfo? CurrentTurnPlayer { get; private set; }

		private TaskCompletionSource<ActionInfo>? abortPlayerTurn;

		public void OnPlayerDisconnect(PlayerHandle player)
		{
			Spectators.Remove(player);
			if (abortPlayerTurn != null && CurrentTurnPlayer?.Name == player.Player.Name)
			{
				abortPlayerTurn.SetResult(new()
				{
					ActionType = PokerAction.Cancelled,
					BetAmount = 0,
				});
			}
		}

		public void AddSpectator(PlayerHandle player, Game game)
		{
			player.Status = PlayerStatus.Spectating;
			player.StartSpectating(player, this, game);
		}

		private bool PerformPlayerTurn(PlayerHandle player, ref int dealEnd, int currentIndx, bool isFirst)
		{
			if (IsClosed) return false;
			if (ActivePot.PlayersInvolved.Length == 1) return true;
			if (player.Status == PlayerStatus.Folded) return false;
			bool forcedAllIn = !ActivePot.IsPlaying(player);
			if (forcedAllIn && Pots.ToArray().GetRelevantPot(player) is var relPot && player.CurrentStake == relPot.MaxPotStake + relPot.StakeOffset) return false;


			int maxBet = ActivePot.GetMaxBet(player);
			CurrentTurnPlayer = player;


			ActionInfo result;
			TurnCancelReason? cancelReason;
			var options = new PokerAction[]
			{
				PokerAction.Fold,
				player.CurrentStake < ActivePot.Stake ? PokerAction.Call : PokerAction.Check,
				PokerAction.Raise
			};

			if (forcedAllIn || maxBet <= 0) options =
			[
				PokerAction.Fold,
				PokerAction.Call
			];



			abortPlayerTurn = new();
			if (!player.IsConnected)
			{
				result = new()
				{
					ActionType = PokerAction.Cancelled
				};
				cancelReason = TurnCancelReason.Disconnect;
			}
			else
			{
				int taskIndx = 0;
				var tasks = new Task<ActionInfo>[]
				{
					PlayerDoesTurnAsync(options, player, isFirst),
					abortPlayerTurn.Task,
					Task.Run(() =>
					{
						Task.Delay(TURN_TIMEOUT).Wait();
						return new ActionInfo() { ActionType = PokerAction.Cancelled, BetAmount = 0 };
					})
				};
				taskIndx = Task.WaitAny(tasks);
				cancelReason = taskIndx > 0 ? Enum.GetValues<TurnCancelReason>()[taskIndx] : null;
				result = tasks[taskIndx].Result;
			}
			abortPlayerTurn = null;

			if (result.ActionType != PokerAction.Cancelled)
			{
				Game.Log($"[SERVER] Player {player.Player} does {result.ActionType}.");
				Inform((p) => p.OtherPlayerDoes(player, result));
			}

			if (IsClosed) return false;

			// I can NOT understand why there is a loop here. AFAIK the game state shouldn't change, no?
			for (int i = 0; i < 2; i++)
			{
				try
				{
					if (!options.Contains(result.ActionType)) throw new InvalidTurnException(player, "Player performed illegal action.");

					switch (result.ActionType)
					{
						case PokerAction.Cancelled:
							Game.Log($"[SERVER] Player {player.Player}'s turn was cancelled due to {cancelReason}.");
							player.Status = PlayerStatus.Folded;
							result = new ActionInfo()
							{
								ActionType = PokerAction.Fold,
							};
							Inform((p) => { p.TurnCanceled(player, cancelReason ?? TurnCancelReason.Other); p.OtherPlayerDoes(player, result); });
							break;
						case PokerAction.Fold:
							RemainingPlayerCount -= 1;
							if (RemainingPlayerCount == 1) return true;
							return false;
						case PokerAction.Check:
							if (player.CurrentStake < ActivePot.Stake) throw new InvalidTurnException(player, "Player performed illegal check.");
							return false;
						case PokerAction.Raise:
							if (!TryBet(player, result.BetAmount, false, forcedAllIn, result.ActionType)) throw new InvalidTurnException(player, "Player performed illegal raise.");
							dealEnd = currentIndx + Players.Length;
							return false;
						case PokerAction.Call:
							if (!TryBet(player, forcedAllIn ? player.Stack : ActivePot.Stake + ActivePot.StakeOffset - player.Player.CurrentStake, false, forcedAllIn, result.ActionType))
								throw new InvalidTurnException(player, "Player performed illegal call.");
							return false;
						default:
							throw new NotImplementedException();
					}
				}
				catch (InvalidTurnException ex)
				{
					player.InformException(ex.ToInfo());
					result = new()
					{
						ActionType = PokerAction.Fold,
						BetAmount = 0,
					};
				}
				catch (Exception ex)
				{
					player.InformException(new SerializedExceptionInfo { Type = ExceptionType.Unknown, Consequence = ExceptionConsequence.DefaultedTurn, Message = "An unexpected error occured: " + ex.GetType() + " " + ex.Message });
					result = new()
					{
						ActionType = PokerAction.Fold,
						BetAmount = 0,
					};
				}
			}
			throw new FatalGameError(ExceptionConsequence.RoundEnd, "Turn Loop did not exit properly");
		}

		public bool IsClosed = false;
		public void Close()
		{
			if (IsClosed) return;
			IsClosed = true;
			Inform((p) =>
			{
				p.RoundClosed();
			});
			Players = [];
		}


		private async Task<ActionInfo> PlayerDoesTurnAsync(PokerAction[] actions, PlayerHandle player, bool isFirst)
		{
			Game.Log($"[SERVER] It's Player {player.Player}'s turn.");

			return await Task.Run(() =>
			{
				Inform((p) => p.PlayersTurn(player, isFirst));
				return player.StartTurn(actions);
			});
		}

	}

	public struct ActionInfo
	{
		public PokerAction ActionType;
		public int BetAmount;
	}

	public enum PokerAction
	{
		Cancelled,
		Fold,
		Check,
		Call,
		Raise
	}

	public enum TurnCancelReason
	{
		Other,
		Disconnect,
		Timeout
	}
}
