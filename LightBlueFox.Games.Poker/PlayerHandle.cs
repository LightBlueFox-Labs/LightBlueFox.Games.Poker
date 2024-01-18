using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker
{
    public abstract class PlayerHandle
    {
        public readonly PlayerInfo Player;

        public PlayerHandle(string name)
        {
            Player = new(name);
        }

        public PlayerStatus Status { get; private set; }

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
    }

    public enum PlayerStatus
    {
        Disconnected,
        NotPlaying,
        Waiting,
        DoesTurn,
        Folded
    }
}
