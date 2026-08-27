namespace Mars.SiteEngine.Abstractions.Exceptions;

public class XTFunctionException : Exception
{
    public XTFunctionException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}
