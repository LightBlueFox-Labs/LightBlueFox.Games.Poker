using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;

namespace LightBlueFox.Games.Poker
{
    [CompositeSerialize]
    public struct PlayerInfo: IEquatable<PlayerInfo>
    {
        public readonly string Name;

        public PlayerInfo(string name) => Name = name;

        public bool Equals(PlayerInfo other)
        {
            return Name == other.Name;
        }

        public override string ToString() => Name;
    }
}