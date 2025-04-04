using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Player;


namespace LightBlueFox.Games.Poker.Evaluation
{
	public struct RoundEndPlayerInfo
    {
        public PlayerInfo Player;
        public Card[]? Cards;
        public HandEvaluation? Evaluation;
        public bool CardsVisible;
        public bool HasFolded;
        public bool HasWon;
        public int ReceivedCoins;
    }
}
