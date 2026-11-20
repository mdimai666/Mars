namespace Mars.Server.Abstractions.Managers;

/// <summary>
/// MQTT-style topic matching. Segments are separated by '/':
/// literal segments match case-insensitively, '*' matches exactly one segment,
/// '**' matches all remaining segments (only valid as the last segment),
/// '[a,b]' matches one of the listed segments.
/// </summary>
public static class TopicMatcher
{
    public static CompiledTopic Compile(string pattern) => new(pattern);

    public static bool IsMatch(string pattern, string topic) => Compile(pattern).IsMatch(topic);
}

/// <summary>
/// Topic pattern parsed once at subscription time; <see cref="IsMatch"/> does no re-parsing of the pattern.
/// </summary>
public sealed class CompiledTopic
{
    private enum SegmentKind : byte { Literal, Single, Multi, Set }

    private readonly struct Segment
    {
        public SegmentKind Kind { get; init; }
        public string Value { get; init; }
        public string[]? Options { get; init; }
    }

    private readonly Segment[] _segments;
    private readonly bool _matchAll;
    private readonly bool _neverMatch;

    public string Pattern { get; }

    /// <summary>
    /// Lowercased first segment when it is a literal (used for routing groups); null for wildcard/set starts.
    /// </summary>
    public string? StartSegment { get; }

    internal CompiledTopic(string pattern)
    {
        Pattern = pattern;

        if (string.IsNullOrWhiteSpace(pattern))
        {
            _neverMatch = true;
            _segments = [];
            return;
        }

        if (pattern == "*")
        {
            _matchAll = true;
            _segments = [];
            return;
        }

        var parts = pattern.Split('/');
        var segments = new Segment[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];

            if (p == "**")
            {
                if (i != parts.Length - 1)
                {
                    _neverMatch = true;
                    _segments = [];
                    return;
                }
                segments[i] = new Segment { Kind = SegmentKind.Multi };
            }
            else if (p == "*")
            {
                segments[i] = new Segment { Kind = SegmentKind.Single };
            }
            else if (p.Length >= 2 && p.StartsWith('[') && p.EndsWith(']'))
            {
                segments[i] = new Segment
                {
                    Kind = SegmentKind.Set,
                    Options = p.Substring(1, p.Length - 2).Split(',', StringSplitOptions.TrimEntries),
                };
            }
            else
            {
                segments[i] = new Segment { Kind = SegmentKind.Literal, Value = p };
            }
        }

        _segments = segments;
        StartSegment = segments[0].Kind == SegmentKind.Literal
            ? segments[0].Value.ToLowerInvariant()
            : null;
    }

    public bool IsMatch(string topic)
    {
        if (_neverMatch) return false;
        if (string.IsNullOrWhiteSpace(topic)) return false;
        if (_matchAll) return true;

        var parts = topic.Split('/');
        if (parts.Length < _segments.Length) return false;

        for (var i = 0; i < _segments.Length; i++)
        {
            var seg = _segments[i];
            var v = parts[i];

            switch (seg.Kind)
            {
                case SegmentKind.Literal:
                    if (!v.Equals(seg.Value, StringComparison.OrdinalIgnoreCase)) return false;
                    break;
                case SegmentKind.Set:
                    if (!seg.Options!.Contains(v, StringComparer.OrdinalIgnoreCase)) return false;
                    break;
                case SegmentKind.Single:
                    break;
                case SegmentKind.Multi:
                    return true;
            }
        }

        return parts.Length == _segments.Length;
    }
}
