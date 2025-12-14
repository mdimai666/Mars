using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Shared.Extensions;

public static class ValidationResultExtensions
{
    public static IDictionary<string, string[]> ToProblemDetailsErrors(
        this IReadOnlyCollection<ValidationResult> errors)
    {
        return errors
            .SelectMany(error =>
                (error.MemberNames?.Any() == true
                    ? error.MemberNames
                    : new[] { string.Empty })
                .Select(member => new
                {
                    Member = member,
                    Message = error.ErrorMessage ?? "Validation error"
                }))
            .GroupBy(x => x.Member)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Message).ToArray()
            );
    }
}
