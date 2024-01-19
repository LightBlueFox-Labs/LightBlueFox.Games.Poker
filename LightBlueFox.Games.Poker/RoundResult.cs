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
        public int ReceivedCoins;
    }

    [CompositeSerialize]
    public class RoundResult
    {
        public Card[] TableCards;
        public RoundEndPlayerInfo[] PlayerInfos;
        public int Pot;

        public RoundResult(Card[] tableCards, RoundEndPlayerInfo[] playerInfos, int pot)
        {
            TableCards = tableCards;
            PlayerInfos = playerInfos;
            Pot = pot;
        }

        public RoundResult() { }

        public static RoundResult DetermineRoundResult(Card[] Table, PlayerHandle[] handles, int Pot)
        {
            int remPlayers = handles.Count((p) => p.Status != PlayerStatus.Folded);
            if (remPlayers == 0) throw new ArgumentException("All players seem to have folded!");
            if (remPlayers == 1) {
                return new RoundResult(Table, handles.Select((p) =>
                {
                    if (p.Status != PlayerStatus.Folded) { p.Stack += Pot; }
                    return new RoundEndPlayerInfo()
                    {
                        Player = p,
                        CardsVisible = false,
                        Cards = new Card[0],
                        HasFolded = p.Status == PlayerStatus.Folded,
                        HasWon = p.Status != PlayerStatus.Folded,
                        Eval = new EvalResult[0],
                        ReceivedCoins = p.Status != PlayerStatus.Folded ? Pot : 0
                    };
                }).ToArray(), Pot);
            }

            return new(Table, HandEvaluation.FindBestHands(handles, Table, Pot), Pot);
        }

        
    }
}
