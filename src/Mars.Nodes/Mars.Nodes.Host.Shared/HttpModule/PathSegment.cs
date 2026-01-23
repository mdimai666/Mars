namespace Mars.Nodes.Host.Shared.HttpModule;

internal readonly struct PathSegment
{
    public string Raw { get; }
    public bool IsParameter { get; }
    public string? ParameterName { get; } // null, если не параметр
    public bool HasConstraint { get; }

    public PathSegment(string segment)
    {
        Raw = segment;
        if (segment.StartsWith('{') && segment.EndsWith('}'))
        {
            IsParameter = true;
            var inner = segment[1..^1];
            var colonIndex = inner.IndexOf(':');
            if (colonIndex >= 0)
            {
                ParameterName = inner[..colonIndex];
                HasConstraint = true;
            }
            else
            {
                ParameterName = inner;
                HasConstraint = false;
            }
        }
        else
        {
            IsParameter = false;
            ParameterName = null;
            HasConstraint = false;
        }
    }
}
