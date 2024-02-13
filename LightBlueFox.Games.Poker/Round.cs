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
        private static int[] _DEALS = { 0, 3, 1, 1 };


        public PlayerHandle[] Players;
        public List<Card> TableCards = new List<Card>(); 
        public DeckGenerator Deck = new DeckGenerator();

        private int notFolded;

        private int ButtonIndex;
        private int SBIndex;
        private int BBIndex;

        private int BB;
        private int SB;

        private int currentStake = 0;
        private int Pot = 0;
        private int minBet = 0;

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
        }


        private RoundResult inform(RoundResult res, ref GameState state)
        {
            state = GameState.NotRunning;
            foreach (var p in Players)
            {
                p.EndRound(res);
                p.ChangePlayer(new(p.Player.Name, p.Player.Stack));
            }
            return res;
        }

        public RoundResult PlayRound(ref GameState state)
        {
            foreach (var p in Players)
            {
                p.StartRound(Deck.PopRandom(2), Array.ConvertAll<PlayerHandle, PlayerInfo>(Players, (p) => p), RoundNR, ButtonIndex, SBIndex, BBIndex);
            }

            // SB Bet
            if (!tryBet(Players[SBIndex], SB, true)) throw new NotImplementedException("Player cannot provide blind!");
            // BB Bet
            if (!tryBet(Players[BBIndex], BB, true)) throw new NotImplementedException("Player cannot provide blind!");

            int roundOffset = (BBIndex + 1) % Players.Length;

            foreach (var d in _DEALS)
            {
                minBet = BB;
                TableCards.AddRange(Deck.PopRandom(d));
                Console.WriteLine($"[SERVER] New deal. Now {TableCards.Count}/5 Cards in the middle.");
                foreach (var p in Players) p.NewCardsDealt(TableCards.ToArray(), minBet);

                notFolded = Players.Count((p) => p.Status != PlayerStatus.Folded);
                
                int dealEnd = Players.Length;
                for (int i = 0; i < dealEnd; i++)
                {
                    if(PerformPlayerTurn(Players[(i + roundOffset) % Players.Length], ref dealEnd, i, i == 0)) return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, Pot), ref state);
				}
                roundOffset = ButtonIndex + 1;
            }
            return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, Pot), ref state);
        }

        public static bool CanBet(PlayerHandle player, int betAmount, int currentStake, int minBet, PokerAction? performedAction = null)
        {
            int level = (currentStake - player.CurrentStake ?? throw new NullReferenceException());
            int absoluteBetSize = (betAmount - level);
            return player.Stack >= betAmount && ((absoluteBetSize == 0 && (performedAction == PokerAction.Call || performedAction == null)) || (absoluteBetSize >= minBet && (performedAction == PokerAction.Raise || performedAction == null)));
        }
        private bool tryBet(PlayerHandle player, int betAmount, bool isBlind, PokerAction? performedAction = null)
        {
            if (!CanBet(player, betAmount, currentStake, minBet, performedAction)) return false;

            int level = (currentStake - player.CurrentStake ?? throw new NullReferenceException());
            int absoluteBetSize = (betAmount - level);
            var newP = player.Player;
            newP.CurrentStake += betAmount;
            Pot += betAmount;
            newP.Stack -= betAmount;
            currentStake += absoluteBetSize;
            player.ChangePlayer(newP);
            if (absoluteBetSize > minBet) minBet = absoluteBetSize;
            foreach (var p in Players) { p.PlayerBet(player, betAmount, isBlind, minBet, currentStake, Pot); }
            return true;
        }

        private bool PerformPlayerTurn(PlayerHandle player, ref int dealEnd, int currentIndx, bool isFirst)
        {
			
			if (player.Status == PlayerStatus.Folded) return false;
            
            
            ActionInfo result;
			var options = new PokerAction[]
			{
				PokerAction.Fold,
				(player.CurrentStake < currentStake ? PokerAction.Call : PokerAction.Check),
				PokerAction.Raise
			};

			if (player.isDisconnected) result = new() { ActionType = PokerAction.Fold };
            else
            {
                Console.WriteLine($"[SERVER] It's Player {player.Player}'s turn.");
                foreach (var p in Players) {
                    p.PlayersTurn(new()
                    {
                        Player = player,
                        NewRound = isFirst
                    });    
                }
                result = player.StartTurn(options);
            }
            Console.WriteLine($"[SERVER] Player {player.Player} does {result.ActionType}.");
            foreach (var p in Players) { p.OtherPlayerDoes(player, result); }

            if (!options.Contains(result.ActionType)) throw new InvalidDataException("Player performed illegal action.");

            switch (result.ActionType)
            {
                case PokerAction.Fold:
					notFolded -= 1;
					if (notFolded == 1) return true;
                    break;
                case PokerAction.Check:
                    if (player.CurrentStake < currentStake) throw new InvalidDataException("Player performed illegal check.");
                    break;
                
                case PokerAction.Raise:
                    if (!tryBet(player, result.BetAmount, false, result.ActionType)) throw new InvalidDataException("Player performed illegal raise.");
                    dealEnd = currentIndx + Players.Length;
                    break;
                case PokerAction.Call:
                    if (!tryBet(player, currentStake - player.Player.CurrentStake, false, result.ActionType)) throw new InvalidDataException("Player performed illegal call.");
                    break;

                default:
                    throw new NotImplementedException();
            }

            return false;
        }


        
    }

    

    [CompositeSerialize]
    public struct ActionInfo {
        public PokerAction ActionType;
        public int BetAmount;
    }

    public enum PokerAction
    {
        Fold,
        Check,
        Call,
        Raise
    }
}
