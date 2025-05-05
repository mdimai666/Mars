namespace Mars.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string? message = "Unauthorized", Exception? innerException = null) : base(message, innerException)
    {
    }
}
