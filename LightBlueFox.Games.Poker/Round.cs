using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Games.Poker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
    public class Round
    {
        private static int[] _DEALS = { 0, 3, 1, 1 };


        public PlayerHandle[] Players = { };
        public List<Card> TableCards = new List<Card>(); 
        public DeckGenerator Deck = new DeckGenerator();

        private int startPlayerIndex = 0;

        public Round(PlayerHandle[] players) {
            Reset(players);
        }

        public void Reset(PlayerHandle[] players)
        {
            Players = players;
        }

        private RoundResult inform(RoundResult res)
        {
            foreach (var p in Players)
            {
                p.EndRound(res);
            }
            return res;
        }

        public RoundResult PlayRound()
        {
            foreach (var p in Players)
            {
                p.StartRound(Deck.PopRandom(2), Array.ConvertAll<PlayerHandle, PlayerInfo>(Players, (p) => p));
            }

            foreach (var d in _DEALS)
            {
                
                // add d cards to the table cards
                TableCards.AddRange(Deck.PopRandom(d));
                Console.WriteLine($"[SERVER] New deal. Now {TableCards.Count}/5 Cards in the middle.");
                foreach (var p in Players) p.TableCardsChanged(TableCards.ToArray());

                List<PlayerHandle> notFolded = Players.Where((p) => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Disconnected).ToList();

                for (int i = 0; i < notFolded.Count; i++)
                {
                    
                    var player = notFolded[(i + startPlayerIndex) % notFolded.Count];
                    Console.WriteLine($"[SERVER] It's Player {player.Player}'s turn.");

                    var result = player.StartTurn(new Action[2]
                    {
                        Action.Check,
                        Action.Fold
                    });

                    Console.WriteLine($"[SERVER] Player {player.Player} does {result.ActionType}.");
                    foreach (var p in Players) { p.OtherPlayerDoes(player, result); }


                    if (result.ActionType == Action.Check) { }
                    else if (result.ActionType == Action.Fold)
                    {
                        if (notFolded.Count == 2)
                        {
                            return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players));
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Player perfomed invalid action.");
                    }

                    
                }

            }
            return inform(RoundResult.DetermineRoundResult(TableCards.ToArray(), Players));
        }
    }

    [CompositeSerialize]
    public struct TurnAction {
        public Action ActionType;
    }

    public enum Action
    {
        Fold,
        Check,
    }
}
