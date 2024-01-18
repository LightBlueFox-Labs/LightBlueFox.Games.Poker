using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Connect.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Utils
{
    public delegate int CustomComparer(EvalResult e1, EvalResult e2);


    public static class HandComparison
    {
        private static readonly Dictionary<PokerHands, CustomComparer> customComparers = new()
        {
            {PokerHands.Straight, CompareStraights },
            {PokerHands.StraightFlush, CompareStraights }
        };

        public static CustomComparer GetComparer(PokerHands hand)
        {
            return customComparers.GetValueOrDefault(hand) ?? CompareAny;
        }

        private static int CompareHandAverages(EvalResult e1, EvalResult e2)
        {
            var getAvg = (List<Card> cc) => (double)cc.Sum((c) => (int)c.Value) / cc.Count;
            var fillCC = (EvalResult e) => e.MainCards.Concat(new List<Card>(e.RemainingCards).GetHighest(5 - e.MainCards.Length)).ToList();


            return getAvg(fillCC(e1)).CompareTo(getAvg(fillCC(e2)));
        }

        private static int CompareAny(EvalResult e1, EvalResult e2)
        {
            int mainComp = new List<Card>(e1.MainCards).GetHighest().CompareTo(new List<Card>(e2.MainCards).GetHighest());
            if (mainComp != 0) return mainComp;

            int mainAvg = CompareHandAverages(e1, e2);
            if (mainAvg != 0) return mainAvg;

            return new List<Card>(new List<Card>(e1.RemainingCards)).GetHighest().CompareTo(new List<Card>(e2.RemainingCards).GetHighest());
        }

        private static int CompareStraights(EvalResult e1, EvalResult e2)
        {

            int cany = CompareAny(e1, e2);

            // This makes sure that any straight with ace is automatically the lowest possible straight
            // by checking if the hand that was previously evaluated as stronger contains an ace. if so automatically return the result opposite of what was previously assumed.
            EvalResult[] evals = { e2, e1 };
            if (cany != 0 && new List<Card>(evals[Math.Clamp(cany, 0, 1)].MainCards).Count((c) => c.Value == CardValue.Ace) > 0) return cany * -1;

            return cany;
        }

    }

    [CompositeSerialize]
    public record struct EvalResult : IComparable<EvalResult>
    {

        public Card[] Combined;
        public Card[] MainCards;
        public Card[] RemainingCards;
        public PokerHands HandType;

        public int CompareTo(EvalResult other)
        {
            if (HandType == other.HandType)
            {
                return HandComparison.GetComparer(HandType)(this, other);
            }

            return HandType.CompareTo(other.HandType);
        }

        public override string ToString()
        {
            return HandType.ToString();
        }
    }


    public static class HandEvaluation
    {
        public record struct HalfResult(List<Card> mainCards, PokerHands handType);

        #region Hand Type Detection
        public static HalfResult? FindStraight(List<Card> cards)
        {;
            cards.Sort();
            List<Card> straight = new();
            straight = ((Func<List<Card>>)(() =>
            {
                foreach (Card c in cards)
                {
                    bool cFits = straight.Count > 0 && straight[straight.Count - 1].Value == c.Value - 1;

                    // If the sequence starts with 2 and cuts of at 5, check if there is an ace in the cards to complete the sequence.
                    if (straight.Count == 4 && straight[0].Value == CardValue.Two && !cFits && cards[^1].Value == CardValue.Ace)
                    {
                        straight.Insert(0, cards[^1]);
                        return straight;
                    }
                    // Try to build a higher straight
                    if (straight.Count >= 5)
                    {
                        // No higher straight, return this one
                        if (!cFits) return straight;

                        straight.Add(c);
                        straight.RemoveAt(0);
                    }
                    // Chain as many cards in a row as possible
                    else if (straight.Count == 0 || cFits)
                    {
                        straight.Add(c);
                    }
                    // Only break of if c isn't one above or the same value as the last card in the sequence. For example, 2 3 3 4 5 6 K should still be a straight.
                    else if (straight.Count > 0 && straight[^1].Value != c.Value)
                    {
                        straight.Clear();
                        straight.Add(c);
                    }
                }
                return straight;
            }))();

            return straight.Count == 5 ? new(straight, PokerHands.Straight) : null;
        }


        public static Card GetHighest(this List<Card> cards) => cards.OrderByDescending(c => c).Take(1).ToArray().First();
        
        public static Card[] GetHighest(this List<Card> cards, int count) => cards.OrderByDescending(c => c).Take(count).ToArray();
        

        public static HalfResult? FindPairCombos(List<Card>[] valueCounts)
        {
            valueCounts = valueCounts.OrderByDescending<List<Card>, int>(x => x == null ? 0 : x.Count).ThenByDescending(cc => cc == null ? 0 : (int)cc.GetHighest().Value).ToArray();

            (int[], PokerHands)[] nrs = new[]
            {
                (new []{4}, PokerHands.FourOfAKind),
                (new []{3, 2}, PokerHands.FullHouse),
                (new []{3}, PokerHands.ThreeOfAKind),
                (new []{2,2}, PokerHands.TwoPair),
                (new []{2}, PokerHands.Pair),
            };

            List<Card> c = null;

            foreach ((int[], PokerHands) n in nrs)
            {
                bool stillTrue = true;
                c = new();

                for (int i = 0; i < n.Item1.Length; i++)
                    if (valueCounts[i] == null || valueCounts[i].Count < n.Item1[i])
                        stillTrue = false;
                    else
                        c.AddRange(valueCounts[i]);

                if (stillTrue)
                {
                    return new(c, n.Item2);
                }
            }
            return null;
        }

        public static HalfResult? FindFlushType(List<Card>[] suitCounts)
        {
            foreach (List<Card> suitlist in suitCounts)
            {
                if (suitlist != null && suitlist.Count >= 5)
                {
                    HalfResult? isStraight = FindStraight(suitlist);
                    if (isStraight is HalfResult straight)
                    {
                        return new(straight.mainCards, straight.mainCards.GetHighest().Value == CardValue.Ace ? PokerHands.RoyalFlush : PokerHands.StraightFlush);
                    }
                    else
                    {
                        return new(new List<Card>(suitlist.GetHighest(5)), PokerHands.Flush);
                    }
                }
            }
            return null;
        }

        public static EvalResult EvaluateHand(Card[] h, Card[] tableCards)
        {

            var result = new EvalResult();
            var cmbIned = h.Concat(tableCards).ToList();
            result.Combined = cmbIned.ToArray();


            List<Card>[] suitCounts = new List<Card>[4];
            List<Card>[] valueCounts = new List<Card>[13];

            foreach (Card c in result.Combined.OrderByDescending(c => c))
            {
                if (suitCounts[(int)c.Suit] == null) suitCounts[(int)c.Suit] = new();
                if (valueCounts[(int)c.Value - 2] == null) valueCounts[(int)c.Value - 2] = new();

                suitCounts[(int)c.Suit].Add(c);
                valueCounts[(int)c.Value - 2].Add(c);
            }

            HalfResult res = FindFlushType(suitCounts) ?? FindPairCombos(valueCounts) ?? new(new List<Card>(new Card[] { result.Combined.OrderByDescending(c => c).First() }), PokerHands.HighCard);
            if (res.handType < PokerHands.Straight) res = FindStraight(cmbIned) ?? res;


            result.MainCards = res.mainCards.ToArray();
            result.HandType = res.handType;
            result.RemainingCards = result.Combined.Except(result.MainCards).ToArray();

            return result;
        }
        #endregion

        public static RoundEndPlayerInfo[] FindBestHands(PlayerHandle[] players, Card[] tableCards)
        {
            var notFolded = players.Where((p) => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Disconnected).ToList();
            if (notFolded.Count < 2) throw new Exception("Too many players folded!");
            
            Dictionary<PlayerHandle, RoundEndPlayerInfo> bestPlayerHands = new ()
            {
                { notFolded[0],
                new()
                {
                    Cards = notFolded[0].Cards,
                    CardsVisible = true,
                    HasWon = true,
                    HasFolded = false,
                    Player = notFolded[0].Player
                } }
            };

            for (int i = 1; i < notFolded.Count; i++)
            {
                if (notFolded[i].Status == PlayerStatus.Folded) continue; 
                int res = CompareHands(players[i].Cards ?? throw new InvalidOperationException("Player has no hand!"), bestPlayerHands.Values.First().Cards, tableCards);
                if (res == 1 || bestPlayerHands.Values.First().HasFolded)
                    bestPlayerHands.Clear();
                if (res >= 0)
                    bestPlayerHands.Add(notFolded[i], new()
                    {
                        Cards = notFolded[i].Cards,
                        CardsVisible = true,
                        HasWon = true,
                        HasFolded = false,
                        Player = notFolded[i].Player,
                        Eval = new EvalResult[] { GetEvaluation(notFolded[i].Cards, tableCards) }
                    });
            }
            List<RoundEndPlayerInfo> infos = new();
            foreach (var looser in players.Except(bestPlayerHands.Keys))
            {
                infos.Add(new()
                {
                    Cards = (looser.Status != PlayerStatus.Folded ? looser.Cards : null) ?? new Card[0] { },
                    CardsVisible = looser.Status != PlayerStatus.Folded ? true : false,
                    HasFolded = looser.Status == PlayerStatus.Folded,
                    HasWon = false,
                    Player = looser.Player,
                    Eval = looser.Status == PlayerStatus.Folded ? new EvalResult[0] : new EvalResult[] { GetEvaluation(looser.Cards, tableCards) }
                });
            }
            infos.AddRange(bestPlayerHands.Values);
            
            return infos.ToArray();
        }

        public static int CompareHands(Card[] h1, Card[] h2, Card[] table)
        {
            return GetEvaluation(h1, table).CompareTo(GetEvaluation(h2, table));
        }

        private static Dictionary<int, (EvalResult result, Card[] hand, Card[] table)> bufferedEvals = new();
        public static EvalResult GetEvaluation(Card[] hand, Card[] tablecards)
        {
            int hash = HashCode.Combine(hand, tablecards);
            if (bufferedEvals.ContainsKey(hash) && ScrambledEquals(bufferedEvals[hash].table, tablecards)) return bufferedEvals[hash].result;

            var res = HandEvaluation.EvaluateHand(hand, tablecards);
            bufferedEvals[hash] = (res, hand, tablecards);

            return res;
        }

        public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }
    }

    public enum PokerHands
    {
        HighCard,
        Pair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalFlush
    }

    public static class PokerHandsExtensions
    {
        public static bool IsStraight(this PokerHands hand)
        {
            return (new PokerHands[] { PokerHands.Straight, PokerHands.StraightFlush }).Contains(hand);
        }
    }

    public enum HandCompRes
    {
        Weaker = -1,
        Equal = 0,
        Stronger = 1
    }
}
