using Microsoft.Extensions.Localization;

namespace Mars.Host.Shared.Interfaces;

public interface IAppFrontLocalizer
{
    IStringLocalizer GetLocalizer(string? locale = null);

    void Refresh();
}
