using LightBlueFox.Games.Poker.Exceptions;
using LightBlueFox.Games.Poker.Player;
using System.Linq;

namespace LightBlueFox.Games.Poker
{
    public class Game(string id, int bigBlind = 10, int smallBlind = 5)
	{
		public readonly string ID = id;
		public readonly int BigBlind = bigBlind;
		public readonly int SmallBlind = smallBlind;


		public int RoundNR { get; private set; } = 0;
		public IReadOnlyList<PlayerHandle> Players => _players.AsReadOnly();
		public GameInfo Info
			=> new()
			{
				ID = ID,
				BigBlind = BigBlind,
				SmallBlind = SmallBlind,
				GameState = State,
				Players = _players.ToInfo(),
			};


		public string LogText = "";
		private int _lastButton = 0;
		private readonly List<PlayerHandle> _players = [];
		private readonly Dictionary<string, (PlayerHandle, Round)> DisconnectedPlayers = [];

		public void AddPlayer(PlayerHandle p)
		{
			if (_players.Any((p2) => p2.Player.Name == p.Player.Name))
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


			bool wasReconnect = false;
			_players.Add(p);
			if (DisconnectedPlayers.GetValueOrDefault(p.Player.Name) is (PlayerHandle, Round) reconnect)
			{
				wasReconnect = true;
				var oldHandle = reconnect.Item1;
				var round = reconnect.Item2;
				var pIndex = Array.IndexOf(round.Players, oldHandle);
				p.IsConnected = true;
				round.Players[pIndex] = p;
				Log("Player {0} reconnected.", p.Player.Name);
				p.Reconnected(oldHandle, reconnect.Item2, this);
				DisconnectedPlayers.Remove(p.Player.Name);
			}
			else
			{
				Log("Player {0} joined.", p.Player.Name);
				p.Stack = 1000;
				p.TellGameInfo(Info);
				CurrentRound?.AddSpectator(p, this);
			}

			foreach (var pl in _players)
			{
				if (pl != p) p.PlayerConnected(pl.Player, false);
				pl.PlayerConnected(p.Player, wasReconnect);
			}
		}

		public void RemovePlayer(PlayerHandle p)
		{
			Log("Player {0} disconnected.", p.Player.Name);
			if (!Players.Contains(p)) return;

			p.IsConnected = false;
			_players.Remove(p);

			foreach (var op in _players)
			{
				op.PlayerDisconnected(p);
			}

			if (CurrentRound != null && CurrentRound.Players.Contains(p))
			{
				CurrentRound.OnPlayerDisconnect(p);
				DisconnectedPlayers.Add(p.Player.Name, (p, CurrentRound));
			}
		}

		public GameState State = GameState.NotRunning;

		public Round? CurrentRound = null;

		public void StartRound()
		{
			if (CurrentRound != null || State == GameState.InRound) throw new InvalidOperationException("Round is already being played!");
			if (_players.Count < 2) throw new InvalidOperationException("Need at least 2 Players to play round!");

			RoundNR++;
			State = GameState.InRound;

			int newButtonIndx = (_lastButton + 1) % _players.Count;
			_lastButton = newButtonIndx;
			CurrentRound = new Round(_players.Where((p) => p.Stack >= BigBlind).ToArray(), SmallBlind, BigBlind, newButtonIndx, RoundNR, this);

			Log("Started round.");
			RunRoundInBackground();
		}

		private async void RunRoundInBackground()
		{
			await Task.Run(() => {
				if (CurrentRound == null) return;

				try
				{
					CurrentRound.PlayRound(ref State);
					DisconnectedPlayers.Clear();


				}
				catch (FatalGameError err)
				{
					if (CurrentRound.IsClosed) return;
					Log($"[FATAL] Fatal game error occurred: {err.Message}");
					foreach (var player in _players)
					{
						player.InformException(err.ToInfo());
					}
				}
				catch (Exception err)
				{
					if (CurrentRound.IsClosed) return;
					Log($"[FATAL] Fatal unknown error ({err.GetType().Name}) occurred: {err.Message}");
					foreach (var player in _players)
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

		public void Log(string msg, params object[] args)
		{
			var line = string.Format(msg, args);
			Console.WriteLine(line);
			LogText += "\n" + line;
		}

		public void Close()
		{
			if (CurrentRound != null)
			{
				CurrentRound.Players = [];
				CurrentRound.IsClosed = true;
			}
			foreach (var p in _players) { p.GameClosed(); }
			DisconnectedPlayers.Clear();
		}
	}


	public enum GameState
	{
		NotRunning,
		Idle,
		InRound,
	}
}
