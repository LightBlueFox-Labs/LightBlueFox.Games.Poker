using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Games.Poker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
    [CompositeSerialize]
    public struct RoundEndPlayerInfo
    {
        public PlayerInfo Player;
        public Card[] Cards;
        public EvalResult[] Eval;
        public bool CardsVisible;
        public bool HasFolded;
        public bool HasWon;
        public int ReceivedCoins;
    }

    [CompositeSerialize]
    public class RoundEndPlayerSummary
    {
        public PlayerInfo Player;
        public int CoinsNet;
        public bool CardsVisible;
        public Card[] Cards = {};
    }
	[CompositeSerialize]
	public struct RoundEndPotInfo
    {
        public RoundEndPlayerInfo[] PlayerInfos;
        public PotInfo Pot;
    }

    [CompositeSerialize]
    public class RoundResult
    {
        public Card[] TableCards;
        public RoundEndPotInfo[] PotResults;
        public RoundEndPlayerSummary[] Summaries;

        public RoundResult(Card[] tableCards, RoundEndPotInfo[] potInfos, RoundEndPlayerSummary[] summaries)
        {
            TableCards = tableCards;
            PotResults = potInfos;
            Summaries = summaries;
        }

        
        public static RoundResult DetermineRoundResult(Card[] Table, PlayerHandle[] handles, PotInfo[] Pots)
        {
            
            int remPlayers = handles.Count((p) => p.Status != PlayerStatus.Folded);
            if (remPlayers == 0) throw new ArgumentException("All players seem to have folded!");
			
			RoundEndPotInfo[] potInfos;
			if (remPlayers == 1) {
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
                            Cards = new Card[0],
                            HasFolded = pl.Status == PlayerStatus.Folded,
                            HasWon = pl.Status != PlayerStatus.Folded,
                            Eval = new EvalResult[0],
                            ReceivedCoins = pl.Status != PlayerStatus.Folded ? pot.TotalPot : 0,
                        };

					}
                }

            }
            else
            {
                potInfos = HandEvaluation.EvaluateAllPots(Pots, Table, handles);
			}



            return new(Table, potInfos, getSummaries(potInfos));
        }

        private static RoundEndPlayerSummary[] getSummaries(RoundEndPotInfo[] infos)
        {
			Dictionary<string, RoundEndPlayerSummary> res = new();
			foreach (RoundEndPotInfo pot in infos)
			{
                foreach(var pi in pot.PlayerInfos)
                {
                    if (!res.ContainsKey(pi.Player.Name))
                    {
                        res.Add(pi.Player.Name, new()
                        {
                            Player = pi.Player,
                            Cards = new Card[] { },
                            CardsVisible=false,
                            CoinsNet = 0
                        });
                    }
                    if (pi.CardsVisible)
                    {
						res[pi.Player.Name].CardsVisible = true;
                        res[pi.Player.Name].Cards = pi.Cards;
					}
                    if(pi.ReceivedCoins != 0)
                    {
						res[pi.Player.Name].CoinsNet += pi.ReceivedCoins;
                        res[pi.Player.Name].Player.Stack += pi.ReceivedCoins;
					}
                    
				}
			}
            return res.Values.ToArray();
		}
    }
}
