using LightBlueFox.Games.Poker.Utils;


namespace LightBlueFox.Games.Poker.PlayerHandles
{
    public class ConsolePlayer : PlayerHandle
    {
        
        public override void OtherPlayerDoes(PlayerInfo playerInfo, TurnAction action)
        {
            Console.WriteLine("[{0}]: Player {1} performed {2}.", this.Player.Name, playerInfo.Name, action.ActionType);
        }

        public override void TableCardsChanged(Card[] cards)
        {
            Console.WriteLine("[{0}]: Table cards changed, are now {1}.", Player.Name, PrintCardCollection(cards));
        }

        protected override TurnAction DoTurn(Action[] actions)
        {
            Console.WriteLine("\n[{0}]: Your turn. You can perform the following actions: ", Player.Name);
            for(int i = 0; i < actions.Length; i++)
            {
                Console.WriteLine(" [{0}] {1}", i, actions[i].Info(CurrentGameStake - Player.CurrentStake));
            }
            Console.Write("Enter action Index: ");
            while (true)
            {
                try
                {
                    var act = new TurnAction()
                    {
                        ActionType = actions[int.Parse(Console.ReadLine() ?? "")]
                    };
                    if(act.ActionType == Action.Raise) act.BetAmount = GetValidBet();
                    Console.WriteLine();
                    return act;
                }
                catch
                {
                    Console.WriteLine("Invalid Input");
                }
            }
        }

        private int GetValidBet()
        {
            Console.Write("You have {0} coins. Enter Bet: ", Stack);
            while (true) {
                try
                {
                    int bet = int.Parse(Console.ReadLine() ?? "") + (CurrentGameStake - Player.CurrentStake);
                    if(!Round.CanBet(this, bet, CurrentGameStake, CurrentMinBet, Action.Raise)) throw new Exception();
                    return bet;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Invalid Bet. MinBet is {CurrentMinBet}.");
                }
            }
        }

        protected override void RoundEnded(RoundResult res)
        {
            Console.WriteLine("[{0}]: Round ended. Pot was {1}", Player.Name, CurrentPot);
            foreach (var i in res.PlayerInfos)
            {
                Console.WriteLine($"     [{i.Player.Name}{(i.Player.Name == Player.Name ? " (YOU)" : "")}] {(i.HasWon ? "won (+" + (i.ReceivedCoins - i.Player.CurrentStake) : "lost (-" + i.Player.CurrentStake )} coins). {(i.CardsVisible ? "Cards: " + PrintCardCollection(i.Cards) : "")}{(i.Eval.Length == 1 ? ", eval: " + i.Eval[0] : "")}");
            }
            Console.WriteLine("------------- ROUND END -----------\n\n");
        }

        protected override void RoundStarted(Card[] cards, PlayerInfo[] info)
        {
            Console.WriteLine("------------ ROUND START ----------");
            Console.WriteLine("[{0}]: Round started. Your cards: {1}", Player.Name, PrintCardCollection(cards));
        }


        private static string[] SUITS = { "S", "H", "C", "D" };
        private static string[] VALS = { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };

        public ConsolePlayer(string name) : base(name)
        {
        }

        private static string PrintCardCollection(Card[] cards)
        {
            List<string> strs = new List<string>();
            foreach (var card in cards)
            {
                strs.Add(VALS[(int)card.Value - 2] + SUITS[(int)card.Suit]);
            }
            return string.Join("-", strs);
        }

        public override void PlayerConnected(PlayerInfo playerInfo)
        {
            Console.WriteLine("[{0}]: New player: {1}", this.Player.Name, playerInfo.Name);
        }

        public override void PlayerDisconnected(PlayerInfo playerInfo)
        {
            Console.WriteLine("[{0}]: Player {1} disconnected.", this.Player.Name, playerInfo.Name);
        }

        protected override void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot)
        {
            Console.WriteLine($"[{Player}]: {player} bet {amount} {(wasBlind ? "(blind)" : "")}. Current Stakes: {currentStake}. Current Pot {currentPot}. New MinBet: {newMinBet}");
        }
    }
}
