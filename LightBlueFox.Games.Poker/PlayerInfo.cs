using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;

namespace LightBlueFox.Games.Poker
{
    [CompositeSerialize]
    public struct PlayerInfo: IEquatable<PlayerInfo>
    {
        public readonly string Name;
        public PlayerStatus Status { get; internal set; }
        public int Stack { get; internal set; }
        public int CurrentStake { get; internal set; }


        public PlayerInfo(string name, int stack) 
        { 
            Name = name;
            Stack = stack;
            Status = PlayerStatus.NotPlaying;
        }

        public bool Equals(PlayerInfo other)
        {
            return Name == other.Name;
        }

        public override string ToString() => Name;
    }
}