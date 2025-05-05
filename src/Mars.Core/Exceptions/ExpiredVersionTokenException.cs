namespace Mars.Core.Exceptions;

public sealed class ExpiredVersionTokenException : Exception
{
    public ExpiredVersionTokenException(string message) : base(message) { }
    public ExpiredVersionTokenException(string message, Exception? innerException) : base(message, innerException) { }
}
