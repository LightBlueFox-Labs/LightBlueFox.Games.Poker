using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Exceptions
{
	public class InvalidTurnException : GameException
	{
		public PlayerInfo Player { get; private set; }

		public InvalidTurnException(PlayerInfo player, string message) : base(message) { Player = player;}

		public override SerializedExceptionInfo ToInfo()
		{
			return new SerializedExceptionInfo()
			{
				Type = ExceptionType.TurnException,
				Consequence = ExceptionConsequence.DefaultedTurn,
			};
		}
	}
}
