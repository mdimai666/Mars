using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/SmtpConfigNode/SmtpConfigNode{.lang}.md")]
public class SmtpConfigNode : ConfigNode
{
    [Required]
    [Display(Name = "Server name")]
    public string ServerName { get; set; } = "";

    public bool SSLRequired { get; set; }

    [Display(Name = "Port")]
    public int Port { get; set; } = 587;

    [Display(Name = "SMTP Authentication Required")]
    public bool SMTPAuthenticationRequired { get; set; }

    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Display(Name = "Password")]
    public string Password { get; set; } = "";
}
