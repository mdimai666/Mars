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

    /// <summary>Базовый радиус скруглений, px (0 = острые углы). Производные: sm=base, md=×1.5, lg=×3.75.</summary>
    public int Radius { get; set; } = 4;

    /// <summary>Толщина обводки контролов (--strokeWidthThin), px.</summary>
    public int StrokeWidth { get; set; } = 1;

    /// <summary>Интенсивность теней, 0..2 (1 = по умолчанию, 0 = без теней).</summary>
    public double ShadowIntensity { get; set; } = 1;
}
