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
            sl.AddSerializers(typeof(PlayerInfo), typeof(ActionInfo), typeof(Card), typeof(EvalResult), typeof(RoundEndPlayerInfo), typeof(RoundResult));
            return new ProtocolDefinition(sl, types.ToArray());
        }

        [Message]
        public struct ReconnectInfo
        {
            public PlayerInfo YourPlayer;
            public Card[] YourCards;
            public GameInfo GameInfo;
            public PlayerInfo[] OtherPlayers;
            public Card[] TableCards;
            public PotInfo[] Pots;
            public int CurrentMinBet;
		}

        [Message]
        public struct NoMoreMoneyMessage
        {

        }

		[Message]
		public struct SpectateInfo
		{
			public PlayerInfo YourPlayer;
			public GameInfo GameInfo;
			public PlayerInfo[] OtherPlayers;
			public Card[] TableCards;
			public PotInfo[] Pots;
			public int CurrentMinBet;
		}

		[Message]
        public struct GameInfo
        {
            public string ID;
            public GameState GameState;

            public PlayerInfo[] Players;
            
            public int BigBlind;
            public int SmallBlind;
        }

        [Message]
        public struct PlayersTurn
        {
            public PlayerInfo Player;
            public bool NewRound;
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

            public PotInfo[] Pots;

            public int TotalStake;
            public int MinBet;
        }

        [Message]
        public struct Ready {
            public string Name;
        }

        [Message]
        public struct PlayerDoesAction
        {
            public PlayerInfo Player;
            public ActionInfo Action;
        }

        [Message]
        public struct DoTurn
        {
            public PokerAction[] PossibleActions;
            public PlayerInfo Player;
        }

        [Message]
        public struct PerformAction
        {
            public ActionInfo Action;
            public PlayerInfo Player;
        }

        [Message]
        public struct NewDealInfo
        {
            public Card[] TableCards;
            public int MinBet;
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
            public bool WasReconnect;
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

            public int RoundNR;
        }
    }
}
