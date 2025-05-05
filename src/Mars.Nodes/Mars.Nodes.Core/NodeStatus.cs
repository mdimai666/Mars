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
    public string Text { get; set; } = "";

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
