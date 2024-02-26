using LightBlueFox.Connect;
using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Games.Poker.Exceptions;
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
                    Player = t.Player,
                    Action = res
                });
            });
        }

        [MessageHandler]
        public static void PlayerConnectedHandler(PlayerConnected pc, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.PlayerConnected(pc.Player, pc.WasReconnect);
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
        public static void NewDealHandler(NewDealInfo ndi, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.NewCardsDealt(ndi.TableCards, ndi.MinBet);
        }

        [MessageHandler]
        public static void RoundStartedHandler(RoundStarted rs, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.StartRound(rs.YourCards, rs.OtherPlayers, rs.RoundNR, rs.BtnIndex, rs.SBIndex, rs.BBIndex);
        }

        [MessageHandler]
        public static void RoundEndedHandler(RoundEnds re, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.EndRound(re.Result);
        }

        [MessageHandler]
        public static void GameInfoResponseHandler(GameInfo gameInfoResponse, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.TellGameInfo(gameInfoResponse);
        }

        [MessageHandler]
        public static void InformPlayerBetHandler(PlayerPlacedBet bet, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.PlayerBet(bet.Player, bet.BetAmount, bet.WasBlind, bet.MinBet, bet.TotalStake, bet.Pots);
        }

        [MessageHandler]
        public static void PlayerInfoChangedHandler(PlayerInfoChanged pic, MessageInfo inf) {
            Receivers[inf.From].MyPlayer.ChangePlayer(pic.Player);
        }

        [MessageHandler]
        public static void PlayersTurnHandler(PlayersTurn pt, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.PlayersTurn(pt);
        }

        private static T[]? nullify<T>(T[] arr)
        {
            return arr.Length == 0 ? null : arr;
        }

        [MessageHandler]
        public static void ReconnectInfoHandler(ReconnectInfo reconnectInfo, MessageInfo inf) {
            Receivers[inf.From].MyPlayer.Reconnected(reconnectInfo.YourPlayer, nullify(reconnectInfo.YourCards), reconnectInfo.GameInfo, reconnectInfo.OtherPlayers, nullify(reconnectInfo.TableCards), nullify(reconnectInfo.Pots), reconnectInfo.CurrentMinBet);
        }

		[MessageHandler]
		public static void SpectateInfoHandler(SpectateInfo spectateInfo, MessageInfo inf)
		{
			Receivers[inf.From].MyPlayer.StartSpectating(spectateInfo.YourPlayer, spectateInfo.GameInfo, spectateInfo.OtherPlayers, nullify(spectateInfo.TableCards), nullify(spectateInfo.Pots), spectateInfo.CurrentMinBet);
		}

        [MessageHandler]
        public static void ExceptionInfoHandler(ExceptionInformation info, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.InformException(info.Info);

            if(info.Info.Consequence == ExceptionConsequence.Kicked || info.Info.Consequence == ExceptionConsequence.GameClose)
            {
                Receivers.Remove(inf.From);
                Receivers[inf.From].Connection.Connection.CloseConnection();
            }
        }

        [MessageHandler]
        public static void GameClosedHandler(GameClosed gc, MessageInfo inf)
        {
            Receivers[inf.From].MyPlayer.GameClosed();
        }

		[MessageHandler]
		public static void RoundClosedHandler(RoundClosed gc, MessageInfo inf)
		{
			Receivers[inf.From].MyPlayer.GameClosed();
		}
	}
}
