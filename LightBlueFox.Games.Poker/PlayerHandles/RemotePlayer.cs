using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightBlueFox.Connect;
using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Games.Poker.Exceptions;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker.PlayerHandles
{
    public class RemotePlayer : PlayerHandle
    {
        private ProtocolConnection _connection;
        public ProtocolConnection? Connection { 
            get { 
                if(isConnected) return _connection;
                else return null;
            } 
            private set { 
                if(value == null) throw new ArgumentNullException("value");
                else _connection = value;
            } 
        }
        private RemotePlayer(ProtocolConnection connection, string name) : base(name)
        {
            _connection = connection;
        }

        private static Dictionary<ProtocolConnection, TaskCompletionSource<string>> WaitingForReady = new();

        [MessageHandler]
        public static void ReadyHandler(Ready r, MessageInfo inf)
        {
            if (WaitingForReady.ContainsKey(inf.From))
            {
                WaitingForReady[inf.From].SetResult(r.Name);
            }
        }

        public static RemotePlayer CreatePlayer(ProtocolConnection c)
        {
            WaitingForReady.Add(c, new());
            string name = WaitingForReady[c].Task.GetAwaiter().GetResult();
            return new RemotePlayer(c, name);
        }
        

        public override void ChangePlayer(PlayerInfo player)
        {
            Connection?.WriteMessage(new PlayerInfoChanged() { Player = player });
            base.ChangePlayer(player);
        }

        public override void OtherPlayerDoes(PlayerInfo playerInfo, ActionInfo action)
        {
            Connection?.WriteMessage<PlayerDoesAction>(new()
            {
                Player = playerInfo,
                Action = action
            });
        }

        protected override void NewDealRound(Card[] cards, int minBet)
        {
            Connection?.WriteMessage<NewDealInfo>(new()
            {
                TableCards = cards,
                MinBet = minBet
            });
        }

        public static Dictionary<PlayerInfo, TaskCompletionSource<ActionInfo>> WaitingForActions = new();


        protected override ActionInfo DoTurn(PokerAction[] actions)
        {
            
            if (WaitingForActions.ContainsKey(this.Player)) throw new InvalidOperationException("Trying to do turn before last turn was finished!");

            Connection?.WriteMessage<DoTurn>(new()
            {
                PossibleActions = actions,
            }) ;
            var res = WaitingForActions[this.Player].Task.GetAwaiter().GetResult();
            WaitingForActions.Remove(this.Player);
            return res;
        }

		public override void TurnCanceled(PlayerInfo player, TurnCancelReason reason)
		{
			if(player.Name == this.Player.Name && WaitingForActions.ContainsKey(player))
            {
                WaitingForActions[player].TrySetResult(new()
                {
                    ActionType = PokerAction.Cancelled,
                    BetAmount = 0
                });
            }
		}

		[MessageHandler]
        public static void HandleTurnAction(PerformAction act, MessageInfo inf)
        {
            if (WaitingForActions.ContainsKey(act.Player))
            {
                WaitingForActions[act.Player].TrySetResult(act.Action);
            }
        }

        protected override void RoundEnded(RoundResult res)
        {
            Connection?.WriteMessage<RoundEnds>(new()
            {
                Result = res
            });
        }

        protected override void RoundStarted(Card[] cards, PlayerInfo[] info, int RoundNR, int btn, int sb, int bb)
        {
            Connection?.WriteMessage<RoundStarted>(new()
            {
                OtherPlayers = info,
                YourCards = cards,
                RoundNR = RoundNR,
                BBIndex = bb,
                BtnIndex = btn,
                SBIndex = sb,
            });
        }

        public override void PlayerConnected(PlayerInfo playerInfo, bool wasReconnect)
        {
            Connection?.WriteMessage<PlayerConnected>(new()
            {
                Player = playerInfo,
                WasReconnect = wasReconnect
            });
        }

        public override void PlayerDisconnected(PlayerInfo playerInfo)
        {
            Connection?.WriteMessage<PlayerDisconnected>(new()
            {
                Player = playerInfo,
            });
        }

        protected override void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int totalStake, PotInfo[] pots)
        {
            Connection?.WriteMessage<PlayerPlacedBet>(new()
            {
                Player = player,
                BetAmount = amount,
                WasBlind = wasBlind,
                MinBet = newMinBet,
                TotalStake = totalStake,
                Pots = pots
            });
        }

		public override void Reconnected(PlayerInfo yourPlayer, Card[]? yourCards, GameInfo gameInfo, PlayerInfo[] otherPlayers, Card[]? tableCards, PotInfo[]? pots, int currentMinBet)
		{
            Connection?.WriteMessage<ReconnectInfo>(new()
            {
                YourPlayer = yourPlayer,
                YourCards = yourCards ?? new Card[] { },
                GameInfo = gameInfo,
                OtherPlayers = otherPlayers,
                TableCards = tableCards ?? new Card[] { },
                Pots = pots ?? new PotInfo[] { },
                CurrentMinBet = currentMinBet
            });
		}
		public override void TellGameInfo(GameInfo gameInfo)
		{
            Connection?.WriteMessage<GameInfo>(gameInfo);
		}

		public override void PlayersTurn(PlayersTurn pt)
		{
			Connection?.WriteMessage(pt);
		}

		public override void StartSpectating(PlayerInfo yourPlayer, GameInfo gameInfo, PlayerInfo[] otherPlayers, Card[]? tableCards, PotInfo[]? pots, int currentMinBet)
		{
			Connection?.WriteMessage<SpectateInfo>(new()
			{
				YourPlayer = yourPlayer,
				GameInfo = gameInfo,
				OtherPlayers = otherPlayers,
				TableCards = tableCards ?? new Card[] { },
				Pots = pots ?? new PotInfo[] { },
				CurrentMinBet = currentMinBet
			});
		}

		public override void InformException(SerializedExceptionInfo exception)
		{
            Connection?.WriteMessage<ExceptionInformation>(new ExceptionInformation()
            {
                Info = exception
            });
		}

		public override void RoundClosed()
		{
            Connection?.WriteMessage<RoundClosed>(new());            
		}

		public override void GameClosed()
		{
			Connection?.WriteMessage<GameClosed>(new());
		}
	}
}
