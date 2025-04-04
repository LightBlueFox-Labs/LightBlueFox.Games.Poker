using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Player;


namespace LightBlueFox.Games.Poker.Evaluation
{


    public class RoundResult
    {
        private RoundResult(Card[] tableCards, RoundEndPotInfo[] potInfos, RoundEndPlayerSummary[] summaries)
        {
            TableCards = tableCards;
            PotResults = potInfos;
            Summaries = summaries;
        }


        public Card[] TableCards;
        public RoundEndPotInfo[] PotResults;
        public RoundEndPlayerSummary[] Summaries;

        public static RoundResult DetermineRoundResult(Card[] Table, PlayerHandle[] handles, PotInfo[] Pots)
        {

            int remPlayers = handles.Count((p) => p.Status != PlayerStatus.Folded);
            if (remPlayers == 0) throw new ArgumentException("All players seem to have folded!");

            RoundEndPotInfo[] potInfos;
            if (remPlayers == 1)
            {
                potInfos = new RoundEndPotInfo[Pots.Length];

                for (int i = 0; i < Pots.Length; i++)
                {
                    var pot = Pots[i];
                    var winner = pot.PlayersInvolved.Where((p) => p.Status != PlayerStatus.Folded).First();


                    potInfos[i] = new()
                    {
                        PlayerInfos = new RoundEndPlayerInfo[pot.PlayersInvolved.Length],
                        Pot = pot
                    };


                    for (int j = 0; j < pot.PlayersInvolved.Length; j++)
                    {
                        var pl = handles.Where((h) => h.Player.Name == pot.PlayersInvolved[j].Name).First();
                        potInfos[i].PlayerInfos[j] = new()
                        {
                            Player = pl,
                            CardsVisible = false,
                            Cards = [],
                            HasFolded = pl.Status == PlayerStatus.Folded,
                            HasWon = pl.Status != PlayerStatus.Folded,
                            Evaluation = null,
                            ReceivedCoins = pl.Status != PlayerStatus.Folded ? pot.TotalPot : 0,
                        };

                    }
                }

            }
            else
            {
                potInfos = EvaluateAllPots(Pots, Table, handles);
            }

            return new(Table, potInfos, GetSummaries(potInfos));
        }

        private static RoundEndPotInfo[] EvaluateAllPots(PotInfo[] pots, Card[] tableCards, PlayerHandle[] players)
        {
            List<RoundEndPotInfo> results = [];
            foreach (var pot in pots)
            {
                var relevantPlayers = players.Where((p) => pot.IsPlaying(p)).ToArray();
                var evaluationsAndPlayers = relevantPlayers
                    .Select(p => (Player: p, Evaluation: HandEvaluation.Evaluate(tableCards, p.Cards)))
                    .OrderByDescending(v => v.Evaluation);

                var winnerEvals = evaluationsAndPlayers.Where(e => e.Evaluation.CompareTo(evaluationsAndPlayers.First().Evaluation) == 0);

                // This rounds down, which is expected. A message for the user could be considered
                int winnings = pot.TotalPot / winnerEvals.Count();

                var winnerInfos = winnerEvals.Select(e => new RoundEndPlayerInfo()
                {
                    Cards = e.Player.Cards,
                    CardsVisible = true,
                    Player = e.Player,
                    HasWon = true,
                    Evaluation = e.Evaluation,
                    ReceivedCoins = winnings
                });

                var looserInfos = evaluationsAndPlayers.Where(e => e.Evaluation.CompareTo(evaluationsAndPlayers.First().Evaluation) == -1).Select(e => new RoundEndPlayerInfo()
                {
                    Cards = e.Player.Status != PlayerStatus.Folded ? e.Player.Cards : null,
                    CardsVisible = e.Player.Status != PlayerStatus.Folded,
                    HasFolded = e.Player.Status == PlayerStatus.Folded,
                    HasWon = false,
                    Player = e.Player,
                    Evaluation = e.Evaluation,
                    ReceivedCoins = 0
                });

                results.Add(new()
                {
                    PlayerInfos = [.. winnerInfos, .. looserInfos],
                    Pot = pot,
                });
            }
            return [.. results];
        }

        private static RoundEndPlayerSummary[] GetSummaries(RoundEndPotInfo[] infos)
        {
            Dictionary<string, RoundEndPlayerSummary> res = new();
            foreach (RoundEndPotInfo pot in infos)
            {
                foreach (var pi in pot.PlayerInfos)
                {
                    if (!res.ContainsKey(pi.Player.Name))
                    {
                        res.Add(pi.Player.Name, new()
                        {
                            Player = pi.Player,
                            Cards = [],
                            CardsVisible = false,
                            CoinsNet = 0
                        });
                    }
                    if (pi.CardsVisible)
                    {
                        res[pi.Player.Name].CardsVisible = true;
                        res[pi.Player.Name].Cards = pi.Cards;
                    }
                    if (pi.ReceivedCoins != 0)
                    {
                        res[pi.Player.Name].CoinsNet += pi.ReceivedCoins;
                        res[pi.Player.Name].Player.Stack += pi.ReceivedCoins;
                    }

                }
            }
            return [.. res.Values];
        }
    }
}
