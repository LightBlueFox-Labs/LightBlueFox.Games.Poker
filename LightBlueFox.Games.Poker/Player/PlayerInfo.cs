using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests.LightBlueFox.Games.Poker")]

namespace LightBlueFox.Games.Poker.Player
{
	public struct PlayerInfo : IEquatable<PlayerInfo>
	{
		public readonly string Name;
		public PlayerStatus Status { get; set; }
		public int Stack { get; set; }
		public int CurrentStake { get; set; }
		public PlayerRole Role { get; set; }
		public bool IsConnected { get; set; } = true;

		public PlayerInfo(string name, int stack)
		{
			Name = name;
			Stack = stack;
			Status = PlayerStatus.NotPlaying;
			Role = PlayerRole.None;
		}

		public bool Equals(PlayerInfo other)
		{
			return Name == other.Name;
		}

		public override string ToString() => Name;
	}

	[Flags]
	public enum PlayerRole
	{
		None = 0,
		Button = 1,
		SmallBlind = 2,
		BigBlind = 4,

		DealerAndSB = 3
	}

	public enum PlayerStatus
	{
		NotPlaying,
		Waiting,
		DoesTurn,
		Folded,

		Spectating
	}
}