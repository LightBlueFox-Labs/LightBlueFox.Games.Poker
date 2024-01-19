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
        private int lastBet = 0;

        public Round(PlayerHandle[] players, int sb, int bb, int buttonInd) {
            Players = players;
            notFolded = players.Length;
            ButtonIndex = buttonInd;
            SBIndex = Players.Length > 2 ? (ButtonIndex + 1) % Players.Length : ButtonIndex; // For Heads-Up, Button and SB coincide.
            BBIndex = (SBIndex + 1) % Players.Length;
            BB = bb;
            SB = sb;
        }


        private RoundResult inform(RoundResult res)
        {
            foreach (var p in Players)
            {
                p.EndRound(res);
                p.ChangePlayer(new(p.Player.Name, p.Player.Stack));
            }
            return res;
        }

        public RoundResult PlayRound()
        {
            foreach (var p in Players)
            {
                p.StartRound(Deck.PopRandom(2), Array.ConvertAll<PlayerHandle, PlayerInfo>(Players, (p) => p));
            }

            // SB Bet
            if (!tryBet(Players[SBIndex], SB, true)) throw new NotImplementedException("Player cannot provide blind!");
            // BB Bet
            if (!tryBet(Players[BBIndex], BB, true)) throw new NotImplementedException("Player cannot provide blind!");

            int roundOffset = (BBIndex + 1) % Players.Length;

            foreach (var d in _DEALS)
            {
                
                TableCards.AddRange(Deck.PopRandom(d));
                Console.WriteLine($"[SERVER] New deal. Now {TableCards.Count}/5 Cards in the middle.");
                foreach (var p in Players) p.TableCardsChanged(TableCards.ToArray());

                List<PlayerHandle> notFolded = Players.Where((p) => p.Status != PlayerStatus.Folded).ToList();

                int dealEnd = Players.Length;
                for (int i = 0; i < dealEnd; i++)
                {
                    PerformPlayerTurn(notFolded[(i + roundOffset) % Players.Length], ref dealEnd, i);
                }
                roundOffset = ButtonIndex + 1;
            }
            return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, Pot));
        }

        public static bool CanBet(PlayerHandle player, int betAmount, int currentStake, int minBet, Action? performedAction = null)
        {
            int level = (currentStake - player.CurrentStake ?? throw new NullReferenceException());
            int absoluteBetSize = (betAmount - level);
            return player.Stack >= betAmount && ((absoluteBetSize == 0 && (performedAction == Action.Call || performedAction == null)) || (absoluteBetSize >= minBet && (performedAction == Action.Raise || performedAction == null)));
        }
        private bool tryBet(PlayerHandle player, int betAmount, bool isBlind, Action? performedAction = null)
        {
            if (!CanBet(player, betAmount, currentStake, lastBet, performedAction)) return false;

            int level = (currentStake - player.CurrentStake ?? throw new NullReferenceException());
            int absoluteBetSize = (betAmount - level);
            var newP = player.Player;
            newP.CurrentStake += betAmount;
            Pot += betAmount;
            newP.Stack -= betAmount;
            currentStake += absoluteBetSize;
            player.ChangePlayer(newP);
            if (absoluteBetSize > lastBet) lastBet = absoluteBetSize;
            foreach (var p in Players) { p.PlayerBet(player, betAmount, isBlind, lastBet, currentStake, Pot); }
            return true;
        }

        private RoundResult? PerformPlayerTurn(PlayerHandle player, ref int dealEnd, int currentIndx)
        {
            if (player.Status == PlayerStatus.Folded) return null;
            Console.WriteLine($"[SERVER] It's Player {player.Player}'s turn.");
            var options = new Action[]
            {
                        (player.CurrentStake < currentStake ? Action.Call : Action.Check),
                        Action.Fold,
                        Action.Raise
            };

            var result = player.StartTurn(options);

            Console.WriteLine($"[SERVER] Player {player.Player} does {result.ActionType}.");
            foreach (var p in Players) { p.OtherPlayerDoes(player, result); }

            if (!options.Contains(result.ActionType)) throw new InvalidDataException("Player performed illegal action.");

            switch (result.ActionType)
            {
                case Action.Fold:
                    if (notFolded == 2) return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players, Pot));
                    break;
                
                case Action.Check:
                    if (player.CurrentStake < currentStake) throw new InvalidDataException("Player performed illegal check.");
                    break;
                
                case Action.Raise:
                    if (!tryBet(player, result.BetAmount, false, result.ActionType)) throw new InvalidDataException("Player performed illegal raise.");
                    dealEnd = currentIndx + Players.Length;
                    break;
                case Action.Call:
                    if (!tryBet(player, currentStake - player.Player.CurrentStake, false, result.ActionType)) throw new InvalidDataException("Player performed illegal call.");
                    break;

                default:
                    throw new NotImplementedException();
            }

            return null;
        }


        
    }

    

    [CompositeSerialize]
    public struct TurnAction {
        public Action ActionType;
        public int BetAmount;
    }

    public enum Action
    {
        Fold,
        Check,
        Call,
        Raise
    }
}
