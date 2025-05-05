using System.ComponentModel.DataAnnotations;
using Mars.Core.Extensions;

namespace Mars.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SlugString : ValidationAttribute
{
    public const string DefaultErrorMessage = "{0} must be a valid slug name [A-z0-9-_.]";
    public bool AllowUpperLetters { get; set; }

    public SlugString() : base(DefaultErrorMessage)
    {

    }

    public override bool IsValid(object? value)
    {
        if (value is not string st) return false;
        if (string.IsNullOrEmpty(st)) return false;
        return Tools.IsValidSlug(st);
    }

    public bool IsValidSlug(string value)
    {
        return AllowUpperLetters
            ? Tools.IsValidSlugWithUpperCase(value)
            : Tools.IsValidSlug(value);
    }
}
