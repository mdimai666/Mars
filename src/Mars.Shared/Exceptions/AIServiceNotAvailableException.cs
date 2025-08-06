namespace Mars.Shared.Exceptions;

public class AIServiceNotAvailableException : Exception
{
    public AIServiceNotAvailableException() : base()
    {
    }

    public AIServiceNotAvailableException(string? message) : base(message)
    {
    }

    public AIServiceNotAvailableException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
