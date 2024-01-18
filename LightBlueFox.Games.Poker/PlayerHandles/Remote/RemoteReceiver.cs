using LightBlueFox.Connect.CustomProtocol.Protocol;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker.PlayerHandles.Remote
{
    public class RemoteReceiver
    {
        public readonly PlayerHandle MyPlayer;
        public readonly ProtocolConnection Connection;

        public RemoteReceiver(PlayerHandle myPlayer, ProtocolConnection connection)
        {
            Receivers.Add(connection, this);
            MyPlayer = myPlayer;
            Connection = connection;
            Connection.WriteMessage<Ready>(new()
            {
                Name = MyPlayer.Player.Name
            });
        }

        private static Dictionary<ProtocolConnection, RemoteReceiver> Receivers = new Dictionary<ProtocolConnection, RemoteReceiver>();

        [MessageHandler]
        public static void DoTurnHandler(DoTurn t, MessageInfo inf)
        {
            Task.Run(() =>
            {
                var recv = Receivers[inf.From];
                var res = recv.MyPlayer.StartTurn(t.PossibleActions);
                recv.Connection.WriteMessage<PerformAction>(new()
                {
                    TurnID = t.TurnID,
                    Action = res
                });
            });
        }

        [MessageHandler]
        public static void PlayerConnectedHandler(PlayerConnected pc, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.PlayerConnected(pc.Player);
        }

        [MessageHandler]
        public static void PlayerDisconnectedHandler(PlayerDisconnected pc, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.PlayerDisconnected(pc.Player);
        }

        [MessageHandler]
        public static void PlayerPerformedActionHandler(PlayerDoesAction action, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.OtherPlayerDoes(action.Player, action.Action);
        }

        [MessageHandler]
        public static void TableCardsChangedHandler(TableCardsChange tcc, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.TableCardsChanged(tcc.TableCards);
        }

        [MessageHandler]
        public static void RoundStartedHandler(RoundStarted rs, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.StartRound(rs.YourCards, rs.OtherPlayers);
        }

        [MessageHandler]
        public static void RoundEndedHandler(RoundEnds re, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.EndRound(re.Result);
        }
    }
}
