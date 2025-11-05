using System.ComponentModel.DataAnnotations;

namespace Mars.Options.Models;

[Display(Name = "Варианты авторизации")]
public class AuthVariantConstOption
{
    public AuthVariants Variants { get; set; }
    public List<SSOProviderInfo> SSOConfigs { get; set; } = new();
    public class SSOProviderInfo
    {
        public string? IconUrl { get; set; }
        public string Label { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string Driver { get; set; } = default!;
    }

    [Flags]
    public enum AuthVariants
    {
        LoginPass,
        SSO
    }
}
