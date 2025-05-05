using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Services.MarsSSOClient;

public class OpenIDAuthForm
{
    [BindProperty(Name = "client_id")] // as form
    [JsonPropertyName("client_id"), Newtonsoft.Json.JsonProperty("client_id")]
    [Required]
    public string ClientId { get; set; }

    [BindProperty(Name = "redirect_uri")]
    [JsonPropertyName("redirect_uri"), Newtonsoft.Json.JsonProperty("redirect_uri")]
    [Required]
    public string RedirectUri { get; set; }

    [BindProperty(Name = "response_type")]
    [JsonPropertyName("response_type"), Newtonsoft.Json.JsonProperty("response_type")]
    [Required]
    public string ResponseType { get; set; } = "code";

    [BindProperty(Name = "state")]
    [JsonPropertyName("state"), Newtonsoft.Json.JsonProperty("state")]
    [Required]
    public string State { get; set; }

    [BindProperty(Name = "scope")]
    [JsonPropertyName("scope"), Newtonsoft.Json.JsonProperty("scope")]
    [JsonRequired]
    [Required]
    public string Scope { get; set; }

    [BindProperty(Name = "grant_type")]
    [JsonPropertyName("grant_type"), Newtonsoft.Json.JsonProperty("grant_type")]
    public string GrantType { get; set; } = "client_credentials";

}

public class OpenIDAuthFormLoginPass: OpenIDAuthForm
{
    [BindProperty(Name = "username")]
    [JsonPropertyName("username"), Newtonsoft.Json.JsonProperty("username")]
    [JsonRequired]
    [Required]
    public string Username { get; set; }

    [BindProperty(Name = "password")]
    [JsonPropertyName("password"), Newtonsoft.Json.JsonProperty("password")]
    [JsonRequired]
    [Required]
    public string Password { get; set; } = "client_credentials";
}

public class OpenIDAuthFormAccessToken: OpenIDAuthForm
{
    [BindProperty(Name = "access_token")]
    [JsonPropertyName("access_token"), Newtonsoft.Json.JsonProperty("access_token")]
    [JsonRequired]
    [Required]
    public string AccessToken { get; set; }

}
