using System.Drawing;

namespace Mars.Nodes.Core.Utils;

public struct MovePoints
{
    public PointF Start { get; set; }
    public PointF End { get; set; }

    public MovePoints(PointF start, PointF end)
    {
        Start = start;
        End = end;
    }

    public float Length()
    {
        return (float)Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
    }

    public override string ToString()
    {
        return $"Segment from {Start} to {End}";
    }
}
