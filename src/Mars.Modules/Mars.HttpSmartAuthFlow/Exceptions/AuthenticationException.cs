namespace Mars.HttpSmartAuthFlow.Exceptions;

public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
    public AuthenticationException(string message, Exception inner) : base(message, inner) { }

    public AuthenticationException() : base()
    {
    }
}
