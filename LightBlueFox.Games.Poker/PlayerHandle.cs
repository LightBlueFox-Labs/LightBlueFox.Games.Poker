﻿using static LightBlueFox.Games.Poker.PlayerHandles.Remote.PokerProtocol;

namespace LightBlueFox.Games.Poker
{
	public abstract class PlayerHandle
	{
		public PlayerInfo Player { get { return _player; } }
		private PlayerInfo _player;

		public bool isDisconnected = false;

		protected int CurrentGameStake;
		protected int CurrentMinBet;
		protected int CurrentPot;

		protected Card[]? TableCards;

		public PlayerHandle(string name)
		{
			_player = new(name, 0);
		}

		public abstract void Reconnect(PlayerHandle newPlayerHandle);


		public PlayerStatus Status
		{
			get { return Player.Status; }
			private set
			{
				var newP = _player;
				newP.Status = value;
				ChangePlayer(newP);
			}
		}

		public int? CurrentStake
		{
			get { return Status == PlayerStatus.NotPlaying ? null : Player.CurrentStake; }
			internal set
			{
				var newP = _player;
				newP.CurrentStake = value ?? throw new ArgumentNullException("Cannot set stake null.");
				ChangePlayer(newP);
			}
		}

		public PlayerRole Role
		{
			get { return Player.Role; }
			set
			{

				var newP = _player;
				newP.Role = value;
				ChangePlayer(newP);
			}
		}


		public int Stack
		{
			get { return Player.Stack; }
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
		public Card[] Cards
		{
			get
			{
				return cards ?? throw new InvalidOperationException("Handle has no cards associated at the moment."); ;
			}
		}

		public void StartRound(Card[] cards, PlayerInfo[] info, int RoundNR, int btn, int sb, int bb)
		{
			this.cards = cards;
			Status = PlayerStatus.Waiting;
			TableCards = new Card[0];
			RoundStarted(cards, info, RoundNR, btn, sb, bb);
		}

		public ActionInfo StartTurn(PokerAction[] possibleActions)
		{
			Status = PlayerStatus.DoesTurn;
			var res = DoTurn(possibleActions);
			Status = res.ActionType == PokerAction.Fold ? PlayerStatus.Folded : PlayerStatus.Waiting;
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

		protected abstract void RoundStarted(Card[] cards, PlayerInfo[] info, int RoundNR, int btnIndex, int sbIndex, int bbIndex);

		protected abstract ActionInfo DoTurn(PokerAction[] actions);

		public void NewCardsDealt(Card[] cards, int minBet)
		{
			CurrentMinBet = minBet;
			TableCards = cards;
			NewDealRound(cards, minBet);
		}

		protected abstract void NewDealRound(Card[] cards, int minBet);

		public abstract void OtherPlayerDoes(PlayerInfo playerInfo, ActionInfo action);

		protected abstract void RoundEnded(RoundResult res);

		public abstract void PlayerConnected(PlayerInfo playerInfo, bool wasReconnect);
		public abstract void PlayerDisconnected(PlayerInfo playerInfo);

		public abstract void PlayersTurn(PlayersTurn pt);

		public abstract void TellGameInfo(GameInfo gameInfo);
		public void PlayerBet(PlayerInfo player, int amount, bool wasBlind, int newMinBet, int currentStake, int currentPot)
		{
			CurrentGameStake = currentStake;
			CurrentPot = currentPot;
			CurrentMinBet = newMinBet;
			PlayerPlacedBet(player, amount, wasBlind, newMinBet, currentStake, currentPot);
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
