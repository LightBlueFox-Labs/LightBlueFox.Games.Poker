using LightBlueFox.Connect.CustomProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker
{
    public class Game
    {
        public readonly string ID;

        public int BigBlind = 5;
        public int SmallBlind = 10;
        private int lastButton = 0;

        private List<PlayerHandle> players = new();
        public IReadOnlyList<PlayerHandle> Players
        {
            get { return players.AsReadOnly(); }
        }

        public void AddPlayer(PlayerHandle p)
        {
            if (players.Any((p2) => p2.Player.Name == p.Player.Name)) throw new ArgumentException("There is already a player with this name!");
            players.Add(p);
            p.Stack = 5000;
            foreach (var pl in players) pl.PlayerConnected(p.Player);
        }

        public void RemovePlayer()
        {
            throw new NotImplementedException();
        }

        public GameState State = GameState.NotRunning;

        public Round? CurrentRound = null;

        public void startRound()
        {
            if (CurrentRound != null || State == GameState.InRound) throw new InvalidOperationException("Round is already being played!");
            if (players.Count < 2) throw new InvalidOperationException("Need at least 2 Players to play round!");


            State = GameState.InRound;

            int newButtonIndx = (lastButton + 1) % players.Count;

            CurrentRound = new Round(players.ToArray(), BigBlind, SmallBlind, newButtonIndx);

            CurrentRound.PlayRound();

            CurrentRound = null;
            State = GameState.Idle;
        }

        public Game(string ID) => this.ID = ID;


        private static Dictionary<ProtocolConnection, Game> getGameForConn = new();
        [MessageHandler]
        public static void HandleGameInfoRequestMessage(GameInfoRequest r, MessageInfo inf)
        {
            var game = getGameForConn[inf.From];
            inf.From.WriteMessage(new GameInfoResponse()
            {
                GameState = game.State,
                BigBlind = game.BigBlind,
                SmallBlind = game.SmallBlind,
                Players = Array.ConvertAll<PlayerHandle, PlayerInfo>(game.Players.ToArray(), (p) => p),
                ID = game.ID,
            });
        }
    }


    public enum GameState
    {
        NotRunning,
        Idle,
        InRound,
    }
}
