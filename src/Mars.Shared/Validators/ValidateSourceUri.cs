using System.ComponentModel.DataAnnotations;
using Mars.Shared.Models;

namespace Mars.Shared.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ValidateSourceUri : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string templateFormat = "/{name}/{method}/...";

        var fname = validationContext.DisplayName ?? validationContext.MemberName;

        if (string.IsNullOrEmpty(value?.ToString())) return ValidationResult.Success;

        if (value is not string) return new ValidationResult($"'{fname}' must be string {templateFormat}");

        try
        {
            new SourceUri(value.ToString());
        }
        catch (Exception ex)
        {
            return new ValidationResult(ex.Message);
        }

        return ValidationResult.Success;
    }
}
