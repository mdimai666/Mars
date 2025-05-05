namespace Mars.Host.Services.Keycloak;

public class BetweenReqData
{
    public required Guid State { get; set; }
    public required string RedirectUrl { get; set; }
    public required string ReturnUrl { get; set; } = "";
}
