using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.SSO.Contracts.Options;

[Display(Name = "OpenID Server")]

public class OpenIDServerOption
{

    public List<OpenIDServerClientConfig> OpenIDClientConfigs { get; set; } = [];

}

public class OpenIDServerClientConfig
{
    [Display(Name = "Включить")]
    public bool Enable { get; set; }

    [Required]
    [SlugString]
    [Display(Name = "ClientId", Description = "the 'client_id'")]
    public string ClientId { get; set; } = default!;

    [Required]
    [Display(Name = "ClientSecretHash", Description = "SHA-256 хэш секрета; plaintext показывается один раз при нажатии 'generate'")]
    public string ClientSecretHash { get; set; } = default!;

    [Display(Name = "CallbackPath")]
    public string CallbackUrl { get; set; } = "https://example.com/dev/Login";

    [Display(Name = "RedirectUris")]
    public string RedirectUris { get; set; } = "*";

    [Display(Name = "RequirePkce")]
    public bool RequirePkce { get; set; }

    [Display(Name = "AllowedGrantTypes", Description = "comma list; etc= authorization_code,refresh_token,password")]
    public string AllowedGrantTypes { get; set; } = "authorization_code,refresh_token,password"; // comma list

    [Display(Name = "RefreshTokenLifetimeDays")]
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
