using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightBlueFox.Connect;
using LightBlueFox.Connect.CustomProtocol.Protocol;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker.PlayerHandles
{
    public class RemotePlayer : PlayerHandle
    {
        private ProtocolConnection _connection;
        public ProtocolConnection? Connection { 
            get { 
                if(!isDisconnected) return _connection;
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
            Console.WriteLine("Player identifies as " + name);
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

        public static Dictionary<uint, TaskCompletionSource<ActionInfo>> WaitingForActions = new();
        public static uint MaxTURNID = 0;


        protected override ActionInfo DoTurn(PokerAction[] actions)
        {
            var id = MaxTURNID++;
            WaitingForActions.Add(id, new TaskCompletionSource<ActionInfo>());
            Connection?.WriteMessage<DoTurn>(new()
            {
                PossibleActions = actions,
                TurnID = id
            }) ;
            return WaitingForActions[id].Task.GetAwaiter().GetResult();   
        }

        [MessageHandler]
        public static void HandleTurnAction(PerformAction act, MessageInfo inf)
        {
            if (WaitingForActions.ContainsKey(act.TurnID))
            {
                WaitingForActions[act.TurnID].SetResult(act.Action);
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

        protected override void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot)
        {
            Connection?.WriteMessage<PlayerPlacedBet>(new()
            {
                Player = player,
                BetAmount = amount,
                WasBlind = wasBlind,
                MinBet = newMinBet,
                CurrentStake = currentStake,
                Pot = currentPot
            });
        }

		public override void Reconnect(PlayerHandle newPlayerHandle)
		{
			if(newPlayerHandle is RemotePlayer rp) Connection = rp.Connection;
		}

		public override void TellGameInfo(GameInfo gameInfo)
		{
            Connection?.WriteMessage<GameInfo>(gameInfo);
		}

		public override void PlayersTurn(PlayersTurn pt)
		{
			Connection?.WriteMessage(pt);
		}
	}
}
