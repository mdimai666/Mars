using System.Drawing;

namespace Mars.Nodes.Core.Utils;

public struct WirePoints
{
    public PointF Start { get; set; }
    public PointF End { get; set; }

    public WirePoints(PointF start, PointF end)
    {
        Start = start;
        End = end;
    }

    // Дополнительные методы, если нужно
    public float Length()
    {
        return (float)Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
    }

    public override string ToString()
    {
        return $"Segment from {Start} to {End}";
    }
}
