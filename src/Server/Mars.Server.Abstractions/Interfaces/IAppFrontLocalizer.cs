using Microsoft.Extensions.Localization;

namespace Mars.Server.Abstractions.Interfaces;

public interface IAppFrontLocalizer
{
    IStringLocalizer GetLocalizer(string? locale = null);

    void Refresh();
}
