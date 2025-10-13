namespace Mars.Core.Exceptions;

public class InternalServerErrorException : Exception
{
    public InternalServerErrorException(string? message = "Internal Server Error", Exception? innerException = null) : base(message, innerException)
    {
    }

    public InternalServerErrorException() : base()
    {
    }

    public InternalServerErrorException(string? message) : base(message)
    {
    }
}
