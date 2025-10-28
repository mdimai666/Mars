using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Services.MarsOpenID;

public class MarsSSOAuthTokenRequest
{
    [BindProperty(Name = "client_id")] // as form
    [JsonPropertyName("client_id")]
    [Required]
    public required string ClientId { get; set; }

    [BindProperty(Name = "client_secret")]
    [JsonPropertyName("client_secret")]
    [Required]
    public required string ClientSecret { get; set; }

    [BindProperty(Name = "code")]
    [JsonPropertyName("code")]
    [Required]
    public required string Code { get; set; }

    [BindProperty(Name = "grant_type")]
    [JsonPropertyName("grant_type")]
    [Required]
    public required string GrantType { get; set; }

    [BindProperty(Name = "redirect_uri")]
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; } = string.Empty;

}
