using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Games.Poker.Utils;

namespace LightBlueFox.Games.Poker.PlayerHandles.Remote
{
    public static class PokerProtocol
    {
        public static ProtocolDefinition BuildProtocol(params Type[] handlerTypes)
        {
            List<Type> types = new List<Type>();
            types.AddRange(handlerTypes);
            types.AddRange(new[] { typeof(PokerProtocol), typeof(RemotePlayer), typeof(RemoteReceiver), typeof(Game) });
            SerializationLibrary sl = new SerializationLibrary();
            sl.AddSerializers(typeof(PlayerInfo), typeof(TurnAction), typeof(Card), typeof(EvalResult), typeof(RoundEndPlayerInfo), typeof(RoundResult));
            return new ProtocolDefinition(sl, types.ToArray());
        }


        [Message]
        public struct GameInfoResponse
        {
            public string ID;
            public GameState GameState;

            public PlayerInfo[] Players;
            
            public int BigBlind;
            public int SmallBlind;

            public PlayerInfo You;
        }

        [Message]
        public struct PlayerInfoChanged
        {
            public PlayerInfo Player;
        }

        [Message]
        public struct GameInfoRequest { }

        [Message]
        public struct PlayerPlacedBet
        {
            public PlayerInfo Player;
            public int BetAmount;
            public bool WasBlind;

            public int CurrentStake;
            public int MinBet;
            public int Pot;
        }

        [Message]
        public struct Ready {
            public string Name;
        }

        [Message]
        public struct PlayerDoesAction
        {
            public PlayerInfo Player;
            public TurnAction Action;
        }

        [Message]
        public struct DoTurn
        {
            public Action[] PossibleActions;
            public uint TurnID;
        }

        [Message]
        public struct PerformAction
        {
            public TurnAction Action;
            public uint TurnID;
        }

        [Message]
        public struct TableCardsChange
        {
            public Card[] TableCards;
        }

        [Message]
        public struct RoundEnds
        {
            public RoundResult Result;
        }

        [Message]
        public struct PlayerConnected
        {
            public PlayerInfo Player;
        }

        [Message]
        public struct PlayerDisconnected
        {
            public PlayerInfo Player;
        }

        [Message]
        public struct RoundStarted
        {
            public Card[] YourCards;
            public PlayerInfo[] OtherPlayers;
            
            public int BtnIndex;
            public int SBIndex;
            public int BBIndex;
        }
    }
}
