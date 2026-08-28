using System.ComponentModel.DataAnnotations;

namespace Mars.Server.Contracts.Options;

[Display(Name = "Настройки Api")]
public class ApiOption
{
    [Display(Name = "Режим просмотра")]
#if DEBUG
    public EViewMode ViewMode { get; set; } = EViewMode.AlwaysShow;
#else
    public EViewMode ViewMode { get; set; } = EViewMode.Auth;
#endif

    public enum EViewMode
    {
        None,
        AlwaysShow,
        Auth,
    }
}
