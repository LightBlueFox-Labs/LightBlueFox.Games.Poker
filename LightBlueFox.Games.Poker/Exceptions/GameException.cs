using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Games.Poker.Exceptions
{
	public abstract class GameException : Exception
	{
		protected GameException()
		{
		}

		protected GameException(string? message) : base(message)
		{
		}

		protected GameException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		protected GameException(string? message, Exception? innerException) : base(message, innerException)
		{
		}

		public abstract SerializedExceptionInfo ToInfo();
	}

	[CompositeSerialize]
	public struct SerializedExceptionInfo
	{
		public string Message;
		public ExceptionType Type;
		public ExceptionConsequence Consequence;
	}

	public enum ExceptionType
	{
		Unknown,
		NoMoreMoney,
		TurnException,
		GameException,
		NameAlreadyTaken,
		JoinGameError
	}

	public enum ExceptionConsequence
	{
		None,
		DefaultedTurn,
		RoundEnd,
		Kicked,
		GameClose
	}
}
