using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Player;


namespace LightBlueFox.Games.Poker.Evaluation
{
	public class RoundEndPlayerSummary
	{
		public PlayerInfo Player;
		public int CoinsNet;
		public bool CardsVisible;
		public Card[]? Cards;
	}
}
