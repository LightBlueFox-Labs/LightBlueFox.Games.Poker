using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Games.Poker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
    public class Round
    {
        public const int TURN_TIMEOUT = 250000;


        private static int[] _DEALS = { 0, 3, 1, 1 };


        public PlayerHandle[] Players;
        public List<PlayerHandle> Spectators = new();
        public List<Card> TableCards = new List<Card>(); 
        public DeckGenerator Deck = new DeckGenerator();

        private List<PotInfo> pots = new List<PotInfo>();

        private PotInfo ActivePot 
        { 
            get
            {
                return pots.Last();
            } 
        }

        private int notFolded;

        private int ButtonIndex;
        private int SBIndex;
        private int BBIndex;

        private int BB;
        private int SB;
        
        private int minBet = 0;
        public int CurrentMinBet { get { return minBet; } }


        private readonly int RoundNR;


		public Round(PlayerHandle[] players, int sb, int bb, int buttonInd, int roundNR) {
            Players = players;
            notFolded = players.Length;
            ButtonIndex = buttonInd;
            SBIndex = Players.Length > 2 ? (ButtonIndex + 1) % Players.Length : ButtonIndex; // For Heads-Up, Button and SB coincide.
            BBIndex = (SBIndex + 1) % Players.Length;
            BB = bb;
            SB = sb;

            if(ButtonIndex == SBIndex)
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
            pots.Add(new(Players.ToInfo(), 0));
        }

        private RoundResult informResult(RoundResult res, ref GameState state)
        {
            state = GameState.NotRunning;
            inform((p) => p.EndRound(res));
            return res;
        }

        private void inform(Action<PlayerHandle> action, Action<PlayerHandle>? spectatorAction = null, bool suppressSpectators = false)
        {
            foreach (var p in Players)
            {
                if(p.isConnected) action(p);
            }
            if (spectatorAction == null) spectatorAction = action; 
            if (!suppressSpectators)                
                foreach (var p in Spectators)
                {
				    if (p.isConnected) spectatorAction(p);
                }
        }

        public RoundResult PlayRound(ref GameState state)
        {
            inform((p) => p.StartRound(Deck.PopRandom(2), Players.ToInfo(), RoundNR, ButtonIndex, SBIndex, BBIndex), null, false);

            // SB Bet
            if (!tryBet(Players[SBIndex], SB, true, false)) throw new NotImplementedException("Player cannot provide blind!");
            // BB Bet
            if (!tryBet(Players[BBIndex], BB, true, false)) throw new NotImplementedException("Player cannot provide blind!");

            int roundOffset = (BBIndex + 1) % Players.Length;

            foreach (var d in _DEALS)
            {
                minBet = BB;
                TableCards.AddRange(Deck.PopRandom(d));
                Console.WriteLine($"[SERVER] New deal. Now {TableCards.Count}/5 Cards in the middle.");

                inform((p) => p.NewCardsDealt(TableCards.ToArray(), minBet));

                notFolded = Players.Count((p) => p.Status != PlayerStatus.Folded);
                
                int dealEnd = Players.Length;
                for (int i = 0; i < dealEnd; i++)
                {
                    if(PerformPlayerTurn(Players[(i + roundOffset) % Players.Length], ref dealEnd, i, i == 0)) return informResult(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, pots.ToArray()), ref state);
				}
                roundOffset = ButtonIndex + 1;
            }
            return informResult(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, pots.ToArray()), ref state);
        }

        public static bool CanBet(PlayerHandle player, int betAmount, int currentStake, int minBet, PokerAction? performedAction = null)
        {
            int level = (currentStake - player.CurrentStake ?? throw new NullReferenceException());
            int absoluteBetSize = (betAmount - level);
            return player.Stack >= betAmount && ((absoluteBetSize == 0 && (performedAction == PokerAction.Call || performedAction == null)) || (absoluteBetSize >= minBet && (performedAction == PokerAction.Raise || performedAction == null)));
        }
        
        private void placePotBetRecursively(int betAmount, PlayerInfo player)
        {
			if (player.CurrentStake > ActivePot.MaxPotStake + ActivePot.StakeOffset)
			{
                int betPcs = betAmount - (player.CurrentStake - (ActivePot.MaxPotStake + ActivePot.StakeOffset));
                ActivePot.TotalPot += betPcs;
				ActivePot.Stake = ActivePot.MaxPotStake;

				pots.Add(new(ActivePot.GetNextPotPlayers(), ActivePot.StakeOffset + ActivePot.MaxPotStake));

                placePotBetRecursively(betAmount - betPcs, player);
			}
			else
			{
				ActivePot.TotalPot += betAmount;
				ActivePot.Stake = player.CurrentStake - ActivePot.StakeOffset;
			}
		}

        private bool tryBet(PlayerHandle player, int betAmount, bool isBlind, bool wasForcedAllIn, PokerAction? performedAction = null)
        {
            if (wasForcedAllIn && performedAction != PokerAction.Call) return false;
            /// HTIS IS SO UGLY PLS FIXXXXXXXXX
            if (!CanBet(player, betAmount, wasForcedAllIn ? player.Stack + player.Player.CurrentStake : ActivePot.Stake + ActivePot.StakeOffset, minBet, performedAction)) return false;

			var newP = player.Player;
            int oldPlayerStake = newP.CurrentStake;
			newP.CurrentStake += betAmount;
			newP.Stack -= betAmount;
			player.ChangePlayer(newP);

			if (wasForcedAllIn)
            {
                int potIndex = pots.Count - 1;
                while (!pots[potIndex].IsPlaying(player)) { potIndex -= 1; }
                pots[potIndex].TotalPot += betAmount;
            }
            else
            {
				int level = ((ActivePot.Stake + ActivePot.StakeOffset) - oldPlayerStake);
				int absoluteBetSize = (betAmount - level);

				placePotBetRecursively(betAmount, newP);
				if (absoluteBetSize > minBet) minBet = absoluteBetSize;
			}

            inform((p) => p.PlayerBet(player, betAmount, isBlind, minBet, ActivePot.Stake + ActivePot.StakeOffset, pots.ToArray()));
            
            
            return true;
        }

        public PlayerInfo? CurrentTurnPlayer { get; private set; }

        private TaskCompletionSource<ActionInfo>? abortPlayerTurn;

        public void OnPlayerDisconnect(PlayerHandle player)
        {
            if(Spectators.Contains(player)) Spectators.Remove(player);
            if(abortPlayerTurn != null && CurrentTurnPlayer?.Name == player.Player.Name)
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
            if (ActivePot.PlayersInvolved.Length == 1) return true;
			if (player.Status == PlayerStatus.Folded) return false;
            bool forcedAllIn = !ActivePot.IsPlaying(player);
            if (forcedAllIn && pots.ToArray().GetRelevantPot(player) is var relPot && player.CurrentStake == relPot.MaxPotStake + relPot.StakeOffset) return false;


            int maxBet = ActivePot.GetMaxBet(player);
            CurrentTurnPlayer = player;
            

            ActionInfo result;
            TurnCancelReason? cancelReason;
			var options = new PokerAction[]
			{
				PokerAction.Fold,
				(player.CurrentStake < ActivePot.Stake ? PokerAction.Call : PokerAction.Check),
				PokerAction.Raise
			};

            if (forcedAllIn || maxBet <= 0) options = new PokerAction[]
            {
                PokerAction.Fold,
                PokerAction.Call
            };


            
			abortPlayerTurn = new();
            if (!player.isConnected)
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
                taskIndx = Task<ActionInfo>.WaitAny(tasks);
                cancelReason = taskIndx > 0 ? Enum.GetValues<TurnCancelReason>()[taskIndx] : null;
				result = tasks[taskIndx].Result;
            }
			abortPlayerTurn = null;


			if (result.ActionType == PokerAction.Cancelled)
            {
				Console.WriteLine($"[SERVER] Player {player.Player}'s turn was cancelled due to {cancelReason}.");
                player.Status = PlayerStatus.Folded;
                result = new ActionInfo()
                {
                    ActionType = PokerAction.Fold,
                };
                inform((p) => { p.TurnCanceled(player, cancelReason ?? TurnCancelReason.Other); p.OtherPlayerDoes(player, result); });
			}
            else
            {
				Console.WriteLine($"[SERVER] Player {player.Player} does {result.ActionType}.");
                inform((p) => p.OtherPlayerDoes(player, result));
			}
            
            

            if (!options.Contains(result.ActionType)) throw new InvalidDataException("Player performed illegal action.");

            switch (result.ActionType)
            {
                case PokerAction.Fold:
					notFolded -= 1;
					if (notFolded == 1) return true;
                    break;
                case PokerAction.Check:
                    if (player.CurrentStake < ActivePot.Stake) throw new InvalidDataException("Player performed illegal check.");
                    break;
                
                case PokerAction.Raise:
                    if (!tryBet(player, result.BetAmount, false, forcedAllIn, result.ActionType)) throw new InvalidDataException("Player performed illegal raise.");
                    dealEnd = currentIndx + Players.Length;
                    break;
                case PokerAction.Call:
                    if (!tryBet(player, forcedAllIn ? player.Stack : ActivePot.Stake + ActivePot.StakeOffset - player.Player.CurrentStake, false, forcedAllIn, result.ActionType)) throw new InvalidDataException("Player performed illegal call.");
                    break;
                default:
                    throw new NotImplementedException();
            }

            return false;
        }

        private async Task<ActionInfo> PlayerDoesTurnAsync(PokerAction[] actions, PlayerHandle player, bool isFirst)
        {
			Console.WriteLine($"[SERVER] It's Player {player.Player}'s turn.");
			
			return await Task.Run(() =>
            {
                inform((p) => p.PlayersTurn(new() { Player = player, NewRound = isFirst }));
				return player.StartTurn(actions);
            });
        }

    }

    

    [CompositeSerialize]
    public struct ActionInfo {
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
