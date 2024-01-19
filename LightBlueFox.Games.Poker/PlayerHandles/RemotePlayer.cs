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
        public readonly ProtocolConnection Connection;
        private RemotePlayer(ProtocolConnection connection, string name) : base(name)
        {
            Connection = connection;
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
            Connection.WriteMessage(new PlayerInfoChanged() { Player = player });
            base.ChangePlayer(player);
        }

        public override void OtherPlayerDoes(PlayerInfo playerInfo, TurnAction action)
        {
            Connection.WriteMessage<PlayerDoesAction>(new()
            {
                Player = playerInfo,
                Action = action
            });
        }

        public override void TableCardsChanged(Card[] cards)
        {
            Connection.WriteMessage<TableCardsChange>(new()
            {
                TableCards = cards
            });
        }

        public static Dictionary<uint, TaskCompletionSource<TurnAction>> WaitingForActions = new();
        public static uint MaxTURNID = 0;


        protected override TurnAction DoTurn(Action[] actions)
        {
            var id = MaxTURNID++;
            WaitingForActions.Add(id, new TaskCompletionSource<TurnAction>());
            Connection.WriteMessage<DoTurn>(new()
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
            Connection.WriteMessage<RoundEnds>(new()
            {
                Result = res
            });
        }

        protected override void RoundStarted(Card[] cards, PlayerInfo[] info)
        {
            Connection.WriteMessage<RoundStarted>(new()
            {
                OtherPlayers = info,
                YourCards = cards
            });
        }

        public override void PlayerConnected(PlayerInfo playerInfo)
        {
            Connection.WriteMessage<PlayerConnected>(new()
            {
                Player = playerInfo,
            });
        }

        public override void PlayerDisconnected(PlayerInfo playerInfo)
        {
            Connection.WriteMessage<PlayerDisconnected>(new()
            {
                Player = playerInfo,
            });
        }

        protected override void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot)
        {
            Connection.WriteMessage<PlayerPlacedBet>(new()
            {
                Player = player,
                BetAmount = amount,
                WasBlind = wasBlind,
                MinBet = newMinBet,
                CurrentStake = currentStake,
                Pot = currentPot
            });
        }
    }
}
