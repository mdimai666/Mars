using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.SSO.Host.OAuth.Dto;

public class OAuthAuthorizeRequest
{
    [JsonPropertyName("client_id")]
    [FromQuery(Name = "client_id")]
    public required string ClientId { get; set; }

    [JsonPropertyName("redirect_uri")]
    [FromQuery(Name = "redirect_uri")]
    public required string RedirectUri { get; set; }

    [JsonPropertyName("scope")]
    [FromQuery(Name = "scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("response_type")]
    [FromQuery(Name = "response_type")]
    public string? ResponseType { get; set; }

    [JsonPropertyName("state")]
    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [JsonPropertyName("nonce")]
    [FromQuery(Name = "nonce")]
    public string? Nonce { get; set; }
}
