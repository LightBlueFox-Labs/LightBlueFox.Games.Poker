using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker
{
    public abstract class PlayerHandle
    {
        public PlayerInfo Player { get { return _player; } }
        private PlayerInfo _player;

        protected int CurrentGameStake;
        protected int CurrentMinBet;
        protected int CurrentPot;
        public PlayerHandle(string name)
        {
            _player = new(name, 0);
        }

        public PlayerStatus Status { get { return Player.Status; } private set
            {
                var newP = _player;
                newP.Status = value;
                ChangePlayer(newP);
            }
        }

        public int? CurrentStake { get { return Status == PlayerStatus.NotPlaying ? null : Player.CurrentStake; }
            internal set
            {
                var newP = _player;
                newP.CurrentStake = value ?? throw new ArgumentNullException("Cannot set stake null.");
                ChangePlayer(newP);
            }
        }

        public int Stack { get { return Player.Stack; }
            internal set
            {
                var newP = _player;
                newP.Stack = value;
                ChangePlayer(newP);
            }
        }

        public virtual void ChangePlayer(PlayerInfo player)
        {
            _player = player;
        }
        
        private Card[]? cards;
        public Card[] Cards { get
            {
                return cards ?? throw new InvalidOperationException("Handle has no cards associated at the moment."); ;
            }
        }

        public void StartRound(Card[] cards, PlayerInfo[] info)
        {
            this.cards = cards;
            Status = PlayerStatus.Waiting;
            RoundStarted(cards, info);
        }

        public TurnAction StartTurn(Action[] possibleActions)
        {
            Status = PlayerStatus.DoesTurn;
            var res = DoTurn(possibleActions);
            Status = res.ActionType == Action.Fold ? PlayerStatus.Folded : PlayerStatus.Waiting;
            return res;
        }

        public void EndRound(RoundResult res)
        {
            Status = PlayerStatus.NotPlaying;
            RoundEnded(res);
            CurrentGameStake = 0;
            CurrentMinBet = 0;
            CurrentPot = 0;
            this.cards = null;
        }

        public static implicit operator PlayerInfo(PlayerHandle handle)
        {
            return handle.Player;
        }

        protected abstract void RoundStarted(Card[] cards, PlayerInfo[] info);

        protected abstract TurnAction DoTurn(Action[] actions);

        public abstract void TableCardsChanged(Card[] cards);

        public abstract void OtherPlayerDoes(PlayerInfo playerInfo, TurnAction action);

        protected abstract void RoundEnded(RoundResult res);

        public abstract void PlayerConnected(PlayerInfo playerInfo);
        public abstract void PlayerDisconnected(PlayerInfo playerInfo);


        public void PlayerBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot)
        {
            CurrentGameStake = currentStake;
            CurrentPot = currentPot;
            CurrentMinBet = newMinBet;
            PlayerPlacedBet(player,amount,wasBlind,newMinBet,currentStake,currentPot);
        }
        protected abstract void PlayerPlacedBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot);
    }

    public enum PlayerStatus
    {
        NotPlaying,
        Waiting,
        DoesTurn,
        Folded
    }
}
