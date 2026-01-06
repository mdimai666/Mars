using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mars.Core.Exceptions;

public class MarsValidationException : ValidationException
{
    [JsonPropertyName("errors")]
    public IDictionary<string, string[]> Errors { get; }

    public MarsValidationException(string? message, IDictionary<string, string[]> errors, Exception? innerException = null) : base(message, innerException)
    {
        Errors = errors;
    }

    public MarsValidationException(IDictionary<string, string[]> errors, Exception? innerException = null) : base("One or more validation errors occurred.", innerException)
    {
        Errors = errors;
    }

    public MarsValidationException(Dictionary<string, string[]> errors, Exception? innerException = null) : base("One or more validation errors occurred.", innerException)
    {
        Errors = errors;
    }

    public static MarsValidationException FromSingleError(string key, string message)
        => new(new Dictionary<string, string[]> { [key] = [message] });
}
