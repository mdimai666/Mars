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

    [Display(Name = "Максимум API-ключей на пользователя")]
    [Range(1, 100)]
    public int ApiKeysMaxPerUser { get; set; } = 10;

    public enum EViewMode
    {
        None,
        AlwaysShow,
        Auth,
    }
}
