using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Services.MarsSSOClient;

public class OpenIDUserInfoRequest
{
    [BindProperty(Name = "client_id")] // as form
    [JsonPropertyName("client_id")]
    [Required]
    public string ClientId { get; set; }

    [BindProperty(Name = "client_secret")]
    [JsonPropertyName("client_secret")]
    [Required]
    public string ClientSecret { get; set; }

    [BindProperty(Name = "user_id")]
    [JsonPropertyName("user_id")]
    [Required]
    public Guid UserId { get; set; }

    public List<KeyValuePair<string, string>> ToForm()
    {
        return new List<KeyValuePair<string, string>>()
        {
            new ("client_id", ClientId),
            new ("client_secret", ClientSecret),
            new ("user_id", UserId.ToString()),
        };
    }
}
