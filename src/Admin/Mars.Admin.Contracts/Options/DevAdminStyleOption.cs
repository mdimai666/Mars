namespace Mars.Admin.Contracts.Options;

public class DevAdminStyleOption
{
    public StylerStyle StylerStyle { get; set; } = new();
}

/// <summary>
/// Настройки темы FluentUI v5 (IThemeService / ThemeSettings).
/// Применяются глобально при старте админки (App.SetupTheme).
/// </summary>
public class StylerStyle
{
    public string BrandColor { get; set; } = "#009d9d";

    /// <summary>-0.5 .. 0.5</summary>
    public double HueTorsion { get; set; }

    /// <summary>-0.5 .. 0.5</summary>
    public double Vibrancy { get; set; }

    public bool IsExact { get; set; }

    /// <summary>ThemeMode: Light | Dark | System</summary>
    public string Mode { get; set; } = "System";
}
