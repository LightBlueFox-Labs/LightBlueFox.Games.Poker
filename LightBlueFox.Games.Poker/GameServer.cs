using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
    public class GameServer
    {
        public readonly string ID;

        private List<PlayerHandle> players = new();
        public IReadOnlyList<PlayerHandle> Players
        {
            get { return players.AsReadOnly(); }
        }

        public void AddPlayer(PlayerHandle p)
        {
            if (players.Count((p2) => p2.Player.Name == p.Player.Name) != 0) throw new ArgumentException("There is already a player with this name!");
            players.Add(p);
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
            CurrentRound = new Round(players.ToArray());

            CurrentRound.PlayRound();

            CurrentRound = null;
            State = GameState.Idle;
        }

        public GameServer(string ID) => this.ID = ID;

    }


    public enum GameState
    {
        NotRunning,
        Idle,
        InRound,
    }
}
