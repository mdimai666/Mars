namespace Mars.HttpSmartAuthFlow.Dto;

public class LoginFormInfo
{
    public string ActionUrl { get; set; } = string.Empty;
    public string UsernameField { get; set; } = string.Empty;
    public string PasswordField { get; set; } = string.Empty;
    public Dictionary<string, string> HiddenFields { get; set; } = [];
}
