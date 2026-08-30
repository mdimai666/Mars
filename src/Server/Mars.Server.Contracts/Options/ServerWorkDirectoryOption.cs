using System.ComponentModel.DataAnnotations;

namespace Mars.Server.Contracts.Options;

[Display(Name = "Рабочая директория сервера")]
public class ServerWorkDirectoryOption
{
    public string WorkDirectory { get; set; } = "";
}
