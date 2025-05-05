namespace Mars.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string? message = "Not found", Exception? innerException = null) : base(message, innerException)
    {
    }
}
