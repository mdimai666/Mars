namespace Mars.Host.Shared.Exceptions;

public class XTFunctionException : Exception
{
    public XTFunctionException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}
