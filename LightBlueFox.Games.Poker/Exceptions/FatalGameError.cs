namespace LightBlueFox.Games.Poker.Exceptions
{
	public class FatalGameError : GameException
	{
		public readonly ExceptionConsequence Consequence;


		public FatalGameError(ExceptionConsequence consequence, string msg) : base(msg)
		{
			Consequence = consequence;
		}

		public override SerializedExceptionInfo ToInfo()
		{
			return new SerializedExceptionInfo()
			{
				Consequence = Consequence,
				Type = ExceptionType.GameException,
				Message = Message
			};
		}
	}
}
