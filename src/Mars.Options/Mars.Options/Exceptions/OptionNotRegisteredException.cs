using Mars.Core.Exceptions;

namespace Mars.Options.Exceptions;

public class OptionNotRegisteredException : NotFoundException
{
    public override string Message => _message;
    readonly string _message;

    public static string OptionNotRegistered(string key) => $"option '{key}' not registered";

    public OptionNotRegisteredException(string optionKeyType)
    {
        _message = OptionNotRegistered(optionKeyType);
    }
}
