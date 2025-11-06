using System.ComponentModel.DataAnnotations;

namespace Mars.Options.Models;

[Display(Name = "Варианты авторизации")]
public class AuthVariantConstOption
{
    public List<SSOProviderInfo> SSOConfigs { get; set; } = [];
    public class SSOProviderInfo
    {
        public string? IconUrl { get; set; }
        public string Label { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string Driver { get; set; } = default!;
    }

}
