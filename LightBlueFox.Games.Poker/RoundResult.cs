using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Games.Poker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    [CompositeSerialize]
    public class RoundResult
    {
        public Card[] TableCards;
        public RoundEndPlayerInfo[] PlayerInfos;

        public RoundResult(Card[] tableCards, RoundEndPlayerInfo[] playerInfos)
        {
            TableCards = tableCards;
            PlayerInfos = playerInfos;
        }

        public RoundResult() { }

        public static RoundResult DetermineRoundResult(Card[] Table, PlayerHandle[] handles)
        {
            List<PlayerHandle> remPlayers = handles.Where((p) => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Disconnected).ToList();
            if (remPlayers.Count == 0) throw new ArgumentException("All players seem to have folded!");
            if (remPlayers.Count == 1) {
                return new RoundResult(Table, handles.Select((p) => new RoundEndPlayerInfo
                {
                    Player = p,
                    CardsVisible = false,
                    Cards = new Card[0],
                    HasFolded = p.Status == PlayerStatus.Folded,
                    HasWon = p.Status != PlayerStatus.Folded,
                    Eval = new EvalResult[0]
                }).ToArray());
            }

            return new(Table, HandEvaluation.FindBestHands(handles, Table));
        }

        
    }
}
