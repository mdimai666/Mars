using System.ComponentModel.DataAnnotations;
using Mars.Core.Features;

namespace Mars.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SlugString : ValidationAttribute
{
    public const string DefaultSlugErrorMessage = "{0} must be a valid slug name [a-z0-9-_.]";
    public const string DefaultSlugWithUpperCaseErrorMessage = "{0} must be a valid slug name [A-Za-z0-9-_.])?";
    public bool AllowUpperLetters { get; }

    public SlugString(bool allowUppercase = false) : base(allowUppercase ? DefaultSlugWithUpperCaseErrorMessage : DefaultSlugErrorMessage)
    {
        AllowUpperLetters = allowUppercase;
    }

    public override bool IsValid(object? value)
    {
        if (value is not string st) return false;
        if (string.IsNullOrEmpty(st)) return false;
        return IsValidSlug(st);
    }

    public bool IsValidSlug(string value)
    {
        return AllowUpperLetters
            ? TextTool.IsValidSlugWithUpperCase(value)
            : TextTool.IsValidSlug(value);
    }
}
