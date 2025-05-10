using Mars.Core.Extensions;

namespace Mars.Nodes.Core;

//class additionsNodeProps
//{
//      category,
//}


public class NodeStatus
{
    /// <summary>
    /// css color
    /// </summary>
    public string Color { get; set; } = "#d3d3d3";
    public EShape Shape { get; set; }
    string _text = "";
    public string Text { get => _text; set => _text = value.TextEllipsis(30); }

    public NodeStatus()
    {

    }

    public NodeStatus(string text, string? color = null, EShape? shape = null)
    {
        Text = text;
        Color = color ?? "#d3d3d3";
        Shape = shape ?? NodeStatus.EShape.Rect;
    }

    public enum EShape
    {
        Rect,
        Trangle,
        Circle,
        Ring
    }
}
