using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.Utils;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker
{
	public class Game
    {
        public readonly string ID;

        public int RoundNR { get; private set; } = 0;

        public int BigBlind = 10;
        public int SmallBlind = 5;
        private int lastButton = 0;

        private List<PlayerHandle> players = new();

        private Dictionary<string, (PlayerHandle, Round)> disconnectedPlayers = new();
        public IReadOnlyList<PlayerHandle> Players
        {
            get { return players.AsReadOnly(); }
        }
        public GameInfo Info
        {
            get
            {
                return new()
                {
                    ID = ID,
                    BigBlind = BigBlind,
                    SmallBlind = SmallBlind,
                    GameState = State,
                    Players = players.ToInfo(),
                };
            }
        }
        public void AddPlayer(PlayerHandle p)
        {
            if (players.Any((p2) => p2.Player.Name == p.Player.Name)) throw new ArgumentException("There is already a player with this name!");


            bool wasReconn = false;
			players.Add(p);
			if (disconnectedPlayers.GetValueOrDefault(p.Player.Name) is (PlayerHandle, Round) reconnect)
            {
                wasReconn = true;
                var oldHandle = reconnect.Item1;
                var round = reconnect.Item2;
                var pIndex = Array.IndexOf(round.Players, oldHandle);
                p.isConnected = true;
                round.Players[pIndex] = p;
                p.Reconnected(oldHandle, reconnect.Item2, this);
                disconnectedPlayers.Remove(p.Player.Name);
            }
            else
            {
				p.Stack = 1000;
                p.TellGameInfo(Info);
				if (CurrentRound != null)
				{
					CurrentRound.AddSpectator(p, this);
				}
			}

            

            foreach (var pl in players)
            {
                if (pl != p) p.PlayerConnected(pl.Player, false);
                pl.PlayerConnected(p.Player, wasReconn);
            }
        }

        public void RemovePlayer(PlayerHandle p)
        {
            if (!Players.Contains(p)) return;

            if(p is RemotePlayer rp && getGameForConn.ContainsKey(rp.Connection)) getGameForConn.Remove(rp.Connection);
			
            p.isConnected = false;
			players.Remove(p);

            foreach (var op in players)
            {
                op.PlayerDisconnected(p);
            }

            if (CurrentRound != null && CurrentRound.Players.Contains(p)) {
                CurrentRound.OnPlayerDisconnect(p);
                disconnectedPlayers.Add(p.Player.Name, (p, CurrentRound));
            }
        }

        public GameState State = GameState.NotRunning;

        public Round? CurrentRound = null;

        public void startRound()
        {
            RoundNR++;
            if (CurrentRound != null || State == GameState.InRound) throw new InvalidOperationException("Round is already being played!");
            if (players.Count < 2) throw new InvalidOperationException("Need at least 2 Players to play round!");


            State = GameState.InRound;

            int newButtonIndx = (lastButton + 1) % players.Count;

            CurrentRound = new Round(players.ToArray(), SmallBlind, BigBlind, newButtonIndx, RoundNR);

            CurrentRound.PlayRound(ref State);
            disconnectedPlayers.Clear();

            CurrentRound = null;
        }

        public Game(string ID) => this.ID = ID;


        private static Dictionary<ProtocolConnection, Game> getGameForConn = new();
        [MessageHandler]
        public static void HandleGameInfoRequestMessage(GameInfoRequest r, MessageInfo inf)
        {
            var game = getGameForConn[inf.From];
            inf.From.WriteMessage(new GameInfo()
            {
                GameState = game.State,
                BigBlind = game.BigBlind,
                SmallBlind = game.SmallBlind,
                Players = game.Players.ToInfo(),
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
