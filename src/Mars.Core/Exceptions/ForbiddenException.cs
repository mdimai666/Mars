namespace Mars.Core.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string? message = "Forbidden", Exception? innerException = null) : base(message, innerException)
    {
    }
}
