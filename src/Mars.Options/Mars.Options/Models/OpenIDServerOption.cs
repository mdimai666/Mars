using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Core.Attributes;

namespace Mars.Options.Models;

[Display(Name = "OpenID Server")]

public class OpenIDServerOption
{

    public List<OpenIDServerClientConfig> OpenIDClientConfigs { get; set; } = new();

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
    [Display(Name = "ClientSecret", Description = "the 'client_secret'")]
    public string ClientSecret { get; set; } = default!;

    [Display(Name = "CallbackPath")]
    public string CallbackUrl { get; set; } = "https://example.com/dev/Login";
}