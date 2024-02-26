using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Games.Poker.Exceptions;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.Utils;
using System.Linq.Expressions;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker
{
	public class Game
    {
        public readonly string ID;

        public int RoundNR { get; private set; } = 0;

        public string LogText = "";

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
            if (players.Any((p2) => p2.Player.Name == p.Player.Name))
            {
                p.InformException(new SerializedExceptionInfo()
                {
                    Type = ExceptionType.NameAlreadyTaken,
                    Consequence = ExceptionConsequence.Kicked,
                    Message = "The name '" + p.Player.Name + "' is already taken."
                });
				Log("Player {0} tried to connect but name already taken.", p.Player.Name);
				return;
            }


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
				Log("Player {0} reconnected.", p.Player.Name);
				p.Reconnected(oldHandle, reconnect.Item2, this);
                disconnectedPlayers.Remove(p.Player.Name);
            }
            else
            {
                Log("Player {0} joined.", p.Player.Name);
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
			Log("Player {0} disconnected.", p.Player.Name);
			if (!Players.Contains(p)) return;

            if(p is RemotePlayer rp && rp.Connection is ProtocolConnection conn && getGameForConn.ContainsKey(conn)) getGameForConn.Remove(conn);
			
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
            if (CurrentRound != null || State == GameState.InRound) throw new InvalidOperationException("Round is already being played!");
			if (players.Count < 2) throw new InvalidOperationException("Need at least 2 Players to play round!");

			RoundNR++;
			State = GameState.InRound;

            int newButtonIndx = (lastButton + 1) % players.Count;
            lastButton = newButtonIndx;
            CurrentRound = new Round(players.Where((p) => p.Stack >= BigBlind).ToArray(), SmallBlind, BigBlind, newButtonIndx, RoundNR, this);

			Log("Started round.");
			runRoundInBackground();
		}

        private async void runRoundInBackground()
        {
            await Task.Run(() => {
                if (CurrentRound == null) return;

                try
                {
					CurrentRound.PlayRound(ref State);
					disconnectedPlayers.Clear();

					
				}catch (FatalGameError err)
                {
                    if (CurrentRound.IsClosed) return;
                    Log($"[FATAL] Fatal game error occurred: {err.Message}");
                    foreach (var player in players)
                    {
                        player.InformException(err.ToInfo());
                    }
                }catch (Exception err)
                {
					if (CurrentRound.IsClosed) return;
					Log($"[FATAL] Fatal unknown error ({err.GetType().Name}) occurred: {err.Message}");
					foreach (var player in players)
					{
						player.InformException(new SerializedExceptionInfo()
                        {
                            Type = ExceptionType.GameException,
                            Message = "An unknown error occurred while playing the round: " + err.GetType().Name + ": " + err.Message,
                            Consequence = ExceptionConsequence.RoundEnd
                        });
					}
				}

				CurrentRound = null;
				for (int i = Players.Count - 1; i >= 0; i--)
				{
					var p = Players[i];

					if (p.Stack < BigBlind)
					{
						RemovePlayer(p);
						p.InformException(new SerializedExceptionInfo()
                        {
                            Type = ExceptionType.NoMoreMoney,
                            Consequence = ExceptionConsequence.Kicked,
                            Message = "You don't have enough money left to continue playing."
                        });
					}
				}

			});
        }

        public Game(string ID) => this.ID = ID;

        public void Log(string msg, params object[] args)
        {
            var line = String.Format(msg, args);
            Console.WriteLine(line);
            LogText += "\n" + line;
        }

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

        public void Close()
        {
            if (CurrentRound != null)
            {
                CurrentRound.Players = new PlayerHandle[0];
                CurrentRound.IsClosed = true;
            }
            foreach(var p in players)
            {
                p.GameClosed();
            }
            disconnectedPlayers.Clear();
        }
    }


    public enum GameState
    {
        NotRunning,
        Idle,
        InRound,
    }
}
