using LightBlueFox.Games.Poker.Cards;
using LightBlueFox.Games.Poker.Player;

namespace LightBlueFox.Games.Poker.Evaluation
{
    public record class HandEvaluation(PlayerInfo Player, Card[] TableCards, Card[] Hand, PokerHandType Type) : IComparable<HandEvaluation>
    {
        public int CompareTo(HandEvaluation? other)
        {
            ArgumentNullException.ThrowIfNull(other, "No reasonable comparison for null type!");
            return Type == other.Type ? Type.Comparator(Hand, other.Hand) : Type.CompareTo(other.Type);
        }

       
    }
}
