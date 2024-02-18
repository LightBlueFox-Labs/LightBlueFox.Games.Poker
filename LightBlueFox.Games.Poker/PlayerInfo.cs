using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests.LightBlueFox.Games.Poker")]

namespace LightBlueFox.Games.Poker
{
    

    [CompositeSerialize]
    public struct PlayerInfo: IEquatable<PlayerInfo>
    {
        public readonly string Name;
        public PlayerStatus Status { get; internal set; }
        public int Stack { get; internal set; }
        public int CurrentStake { get; internal set; }
        public PlayerRole Role { get; internal set; }
        public bool IsAllIn { get; internal set; }

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
}