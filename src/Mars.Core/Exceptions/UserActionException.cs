namespace Mars.Core.Exceptions;

public sealed class UserActionException : Exception
{
    public string[]? DetailMessages { get; }

    public UserActionException(string message) : base(message) { }
    public UserActionException(string message, string[]? detailMessages, Exception? innerException = null) : base(message, innerException)
    {
        DetailMessages = detailMessages;
    }
}
