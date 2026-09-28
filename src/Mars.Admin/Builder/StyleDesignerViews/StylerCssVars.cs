using System.Globalization;
using System.Text;
using Mars.Admin.Contracts.Options;

namespace Mars.Admin.Builder.StyleDesignerViews;

/// <summary>
/// Скаляры StylerStyle → CSS-переменные. Глобально пишется минимум (--mars-radius-base,
/// --mars-stroke-hairline, --mars-shadow-alpha) — производные считает base.less через calc.
/// Для скоуп-превью производные дублируются явно + fluent-токены: important-мост на html
/// в поддереве не перерезолвится, поэтому preview-контейнеру нужен готовый набор.
/// </summary>
public static class StylerCssVars
{
    public static string GlobalVars(StylerStyle s) => new StringBuilder()
        .Var("--mars-radius-base", Px(s.Radius))
        .Var("--mars-stroke-hairline", Px(s.StrokeWidth))
        .Var("--mars-shadow-alpha", Num(s.ShadowIntensity))
        .ToString();

    public static string PreviewVars(StylerStyle s)
    {
        double md = s.Radius * 1.5, lg = s.Radius * 3.75;
        return new StringBuilder()
            .Var("--mars-radius-base", Px(s.Radius))
            .Var("--mars-radius-sm", Px(s.Radius))
            .Var("--mars-radius-md", Px(md))
            .Var("--mars-radius-lg", Px(lg))
            .Var("--mars-radius-full", "9999px")
            .Var("--mars-stroke-hairline", Px(s.StrokeWidth))
            .Var("--mars-shadow-alpha", Num(s.ShadowIntensity))
            .Var("--mars-shadow-sm", Shadow("0 1px 2px", 0.05, s.ShadowIntensity))
            .Var("--mars-shadow-md", Shadow("0 4px 6px", 0.07, s.ShadowIntensity))
            .Var("--mars-shadow-lg", Shadow("0 10px 15px", 0.1, s.ShadowIntensity))
            .Var("--borderRadiusNone", "0px")
            .Var("--borderRadiusSmall", Px(s.Radius / 2.0))
            .Var("--borderRadiusMedium", Px(s.Radius))
            .Var("--borderRadiusLarge", Px(md))
            .Var("--borderRadiusXLarge", Px(lg))
            .Var("--borderRadiusCircular", "9999px")
            .Var("--strokeWidthThin", Px(s.StrokeWidth))
            .Var("--shadow2", Shadow("0 1px 2px", 0.05, s.ShadowIntensity))
            .Var("--shadow4", Shadow("0 4px 6px", 0.07, s.ShadowIntensity))
            .Var("--shadow8", Shadow("0 4px 6px", 0.07, s.ShadowIntensity))
            .Var("--shadow16", Shadow("0 10px 15px", 0.1, s.ShadowIntensity))
            .Var("--shadow28", Shadow("0 10px 15px", 0.1, s.ShadowIntensity))
            .Var("--shadow64", Shadow("0 10px 15px", 0.1, s.ShadowIntensity))
            .ToString();
    }

    static StringBuilder Var(this StringBuilder sb, string name, string value) =>
        sb.Append(name).Append(':').Append(value).Append(';');

    static string Px(double v) => Num(v) + "px";

    static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    static string Shadow(string shape, double baseAlpha, double intensity) =>
        $"{shape} rgba(0, 0, 0, {Num(baseAlpha * intensity)})";
}
