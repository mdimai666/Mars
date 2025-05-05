namespace Mars.Options.Models;

public class DevAdminStyleOption
{
    public StylerStyle StylerStyle { get; set; } = new();
}

public class StylerStyle
{
    //public string AccentBaseColor { get; set; } = "#0078d4"; //default fluent color
    public string AccentBaseColor { get; set; } = "#009d9d";
    public string NeutralBaseColor { get; set; } = "#808080";
    public string FillColor { get; set; } = "#fbfbfb";
    public bool NoPaint { get; set; }
    public int BaseHeightMultiplier { get; set; } = 8;
    public int BaseHorizontalSpacingMultiplier { get; set; } = 3;
    public int ControlCornerRadius { get; set; } = 4;
    public int LayerCornerRadius { get; set; } = 8;
    public int StrokeWidth { get; set; } = 1;
    public int FocusStrokeWidth { get; set; } = 1;
    public int Density { get; set; } = 0;
    public int DesignUnit { get; set; } = 4;

    public float BaseLayerLuminance { get; set; } = 0.98f;
}
