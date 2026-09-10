namespace Mars.SiteEngine.Abstractions.Templators;

public class XTFunctionException : Exception
{
    public XTFunctionException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}
