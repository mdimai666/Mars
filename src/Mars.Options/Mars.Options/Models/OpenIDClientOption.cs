using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Core.Attributes;

namespace Mars.Options.Models;

[Display(Name = "OpenID Client")]

public class OpenIDClientOption
{

    public List<OpenIDClientConfig> OpenIDClientConfigs { get; set; } = new();

    [JsonIgnore]
    public static Dictionary<string,string> DriverList { get; set; } = new()
    {
        ["mars"] = "Mars",
        ["keycloak"] = "Keycloak",
        ["esia"] = "Esia",
    };
}

public class OpenIDClientConfig
{
    [Display(Name = "Display Title", Description = "Display name of the IdP")]
    [Required]
    public string Title { get; set; } = default!;

    [Display(Name = "Slug", Description = "key name")]
    [SlugString]
    [Required]
    public string Slug { get; set; } = default!;

    [Display(Name = "IconUrl")]
    public string IconUrl { get; set; } = "";

    [Display(Name = "Driver")]
    public string Driver { get; set; } = "";

    [Display(Name = "Включить")]
    public bool Enable { get; set; }

    [Display(Name = "oauth2_auth_endpoint")]
    [Required]
    public string AuthEndpoint { get; set; } = default!;

    [Display(Name = "oauth2_token_endpoint")]
    [Required]
    public string TokenEndpoint { get; set; } = default!;

    [Display(Name = "ClientId", Description = "the 'client_id'")]
    public string ClientId { get; set; } = default!;

    [Display(Name = "ClientSecret", Description = "the 'client_secret'")]
    public string ClientSecret { get; set; } = default!;

    [Display(Name = "CallbackPath", Description = "The request path within the application's base path where the user-agent will be returned. The middleware will process this request when it arrives.")]
    public string CallbackPath { get; set; } = "/signin-oidc"; // /signin-oidc

    //[Display(Name = "SignedOutCallbackPath", Description = "The request path within the application's base path where the user agent will be returned after sign out from the identity provider. See post_logout_redirect_uri from http://openid.net/specs/openid-connect-session-1_0.html#RedirectionAfterLogout")]
    //public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc"; // /signout-callback-oidc

    //[Display(Name = "SignedOut Redirect Uri", Description = "The uri where the user agent will be redirected to after application is signed out from the identity provider. The redirect will happen after the SignedOutCallbackPath is invoked.")]
    //public string SignedOutRedirectUri { get; set; } = "/"; // /

    [Display(Name = "Scopes", Description = "extra scopes except openid and profile")]
    public string Scopes { get; set; } = "openid email profile";

    [Display(Name = "Issuer", Description = "Базовый идентификатор (URI) провайдера, указывающий, кто выдал токен.")]
    [Required]
    public string Issuer { get; set; } = "";
}
