using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker
{
    public class GameInfo
    {
        public required string ID;
        public required GameState GameState;
        public required PlayerInfo[] Players;
        public required int BigBlind;
        public required int SmallBlind;
    }

}
