namespace Mars.PxBlocks.Workspace;

public static class PxBlockSvgHelper
{
    public const float CornerRadius = 5f;
    public const float NotchWidth = 10f;
    public const float NotchHeight = 5f;
    public const float RowHeight = 30f;
    public const float FieldPadding = 6f;
    public const float StatementIndent = 22f;
    public const float ValueOutputWidth = 8f;
    public const float ValueOutputHeight = 16f;
    public const float MinBlockWidth = 60f;
    public const float ShadowHeight = 3f;

    public static (float Width, float Height) MeasureBlock(string text, int fieldCount, int inputCount)
    {
        float width = Math.Max(MinBlockWidth, EstimateTextWidth(text, fieldCount) + FieldPadding * 2 + 20f);
        float height = RowHeight + inputCount * RowHeight;
        return (width, height);
    }

    private static float EstimateTextWidth(string text, int fieldCount)
    {
        float w = 0;
        foreach (char c in text)
            w += c switch { ' ' => 3f, 'i' or 'l' or 'I' => 4f, 'm' or 'w' or 'M' or 'W' => 9f, _ => 7f };
        return w + fieldCount * FieldPadding;
    }

    // --- Shadow path (darker strip at bottom of body, above notch) ---

    public static string ShadowPath(float width, float bodyHeight)
    {
        float r = CornerRadius;
        float sh = ShadowHeight;
        float top = bodyHeight - sh;

        var p = new PathBuilder();
        p.M(r, top);
        p.L(width - r, top);
        p.A(r, r, 0, 0, 1, width, top + r);
        p.L(width, bodyHeight);
        p.L(0, bodyHeight);
        p.L(0, top + r);
        p.A(r, r, 0, 0, 1, r, top);
        p.Z();
        return p.ToString();
    }

    // --- Highlight path (lighter strip at top) ---

    public static string HighlightPath(float width, float notchY)
    {
        float r = CornerRadius;
        float hh = 3f;

        var p = new PathBuilder();
        p.M(r, 0);
        p.L(width - r, 0);
        p.A(r, r, 0, 0, 1, width, r);
        p.L(width, r + hh);
        p.L(0, r + hh);
        p.L(0, r);
        p.A(r, r, 0, 0, 1, r, 0);
        p.Z();
        return p.ToString();
    }

    // --- Notch-only path (the tab at bottom / notch at top drawn separately for better styling) ---

    public static string BottomNotchPath(float width, float y, float totalHeight)
    {
        float nw = NotchWidth;
        float cx = width / 2;

        var p = new PathBuilder();
        p.M(cx - nw / 2, y);
        p.L(cx - nw / 2, totalHeight);
        p.L(cx + nw / 2, totalHeight);
        p.L(cx + nw / 2, y);
        p.Z();
        return p.ToString();
    }

    public static string TopNotchPath(float width)
    {
        float nw = NotchWidth;
        float nh = NotchHeight;
        float cx = width / 2;

        var p = new PathBuilder();
        p.M(cx - nw / 2, 0);
        p.L(cx - nw / 2, nh);
        p.L(cx + nw / 2, nh);
        p.L(cx + nw / 2, 0);
        p.Z();
        return p.ToString();
    }

    // --- Main block paths ---

    /// <summary>
    /// Statement block body. Notch extends below 'height' by NotchHeight.
    /// Total visual height = height + NotchHeight (bottom tab).
    /// Top notch goes above the body (y=0 to y=NotchHeight).
    /// </summary>
    public static string StatementBlockPath(float width, float height, bool hasStatementInput)
    {
        float r = CornerRadius;
        float nh = NotchHeight;
        float si = hasStatementInput ? StatementIndent : 0;

        float cx = width / 2;
        float notchLeft = cx - NotchWidth / 2;
        float notchRight = cx + NotchWidth / 2;
        float totalH = height + nh;

        var p = new PathBuilder();

        // Start after top-left notch
        p.M(0, nh + r);

        // Top-left corner
        p.A(r, r, 0, 0, 1, r, nh);

        // Top edge left of notch
        p.L(notchLeft, nh);

        // Notch indent (top) — goes UP from body
        p.L(notchLeft, 0);
        p.L(notchRight, 0);
        p.L(notchRight, nh);

        // Top edge right of notch
        p.L(width - r, nh);

        // Top-right corner
        p.A(r, r, 0, 0, 1, width, nh + r);

        // Right edge down
        if (hasStatementInput)
        {
            float cStart = nh + r + 4;
            float cEnd = height - r - 4;
            p.L(width, cStart);
            p.L(width - si, cStart);
            p.L(width - si, cEnd);
            p.L(width, cEnd);
        }

        p.L(width, height - r);

        // Bottom-right corner
        p.A(r, r, 0, 0, 1, width - r, height);

        // Bottom edge right of notch
        p.L(notchRight, height);

        // Notch tab (bottom) — goes DOWN from body
        p.L(notchRight, totalH);
        p.L(notchLeft, totalH);
        p.L(notchLeft, height);

        // Bottom edge left of notch
        p.L(r, height);

        // Bottom-left corner
        p.A(r, r, 0, 0, 1, 0, height - r);

        // Left edge up
        p.L(0, nh + r);

        p.Z();
        return p.ToString();
    }

    /// <summary>
    /// Hat block body (rounded top). Bottom tab extends below 'height' by NotchHeight.
    /// </summary>
    public static string HatBlockPath(float width, float height, bool hasStatementInput)
    {
        float r = CornerRadius;
        float nh = NotchHeight;
        float si = hasStatementInput ? StatementIndent : 0;

        float cx = width / 2;
        float domeHeight = r * 2.5f;
        float totalH = height + nh;

        var p = new PathBuilder();

        p.M(cx, 0);

        // Left dome
        p.C(cx - r * 4, 0, 0, domeHeight, 0, domeHeight + r);

        // Left edge down
        p.L(0, height - r);

        // Bottom-left corner
        p.A(r, r, 0, 0, 1, r, height);

        // Bottom edge
        p.L(cx - NotchWidth / 2, height);

        // Notch tab
        p.L(cx - NotchWidth / 2, totalH);
        p.L(cx + NotchWidth / 2, totalH);
        p.L(cx + NotchWidth / 2, height);

        // Bottom edge right
        p.L(width - r, height);

        // Bottom-right corner
        p.A(r, r, 0, 0, 1, width, height - r);

        // Right edge up
        if (hasStatementInput)
        {
            float cStart = domeHeight + r + 4;
            float cEnd = height - r - 4;
            p.L(width, cEnd);
            p.L(width - si, cEnd);
            p.L(width - si, cStart);
            p.L(width, cStart);
        }

        p.L(width, domeHeight + r);

        // Right dome
        p.C(width, domeHeight, cx + r * 4, 0, cx, 0);

        p.Z();
        return p.ToString();
    }

    /// <summary>
    /// Value/reporter block (output connector on left). No notches.
    /// </summary>
    public static string ValueBlockPath(float width, float height)
    {
        float r = CornerRadius;
        float vo = ValueOutputWidth;
        float voHalf = ValueOutputHeight / 2;

        float connX = -vo;
        float connCenterY = height / 2;

        var p = new PathBuilder();

        p.M(connX + r, 0);

        // Top edge
        p.L(width - r, 0);

        // Top-right corner
        p.A(r, r, 0, 0, 1, width, r);

        // Right edge
        p.L(width, height - r);

        // Bottom-right corner
        p.A(r, r, 0, 0, 1, width - r, height);

        // Bottom edge
        p.L(connX + r, height);

        // Bottom-left corner
        p.A(r, r, 0, 0, 1, connX, height - r);

        // Left edge up to connector bottom
        p.L(connX, connCenterY + voHalf);

        // Output connector tab
        p.L(connX - vo, connCenterY + voHalf);
        p.L(connX - vo, connCenterY - voHalf);
        p.L(connX, connCenterY - voHalf);

        // Left edge up
        p.L(connX, r);

        // Top-left corner
        p.A(r, r, 0, 0, 1, connX + r, 0);

        p.Z();
        return p.ToString();
    }

    /// <summary>
    /// Statement input socket (C-gap indicator).
    /// </summary>
    public static string StatementSocketPath(float width, float startY, float endY)
    {
        float si = StatementIndent;
        var p = new PathBuilder();
        p.M(width - si + 4, startY + 4);
        p.L(width - 4, startY + 4);
        p.L(width - 4, endY - 4);
        p.L(width - si + 4, endY - 4);
        p.Z();
        return p.ToString();
    }
}

internal class PathBuilder
{
    private readonly List<string> _parts = [];

    public PathBuilder M(float x, float y) { _parts.Add($"M{x:F1},{y:F1}"); return this; }
    public PathBuilder L(float x, float y) { _parts.Add($"L{x:F1},{y:F1}"); return this; }
    public PathBuilder A(float rx, float ry, float rot, int largeArc, int sweep, float x, float y)
    { _parts.Add($"A{rx:F1},{ry:F1} {rot} {largeArc},{sweep} {x:F1},{y:F1}"); return this; }
    public PathBuilder C(float x1, float y1, float x2, float y2, float x, float y)
    { _parts.Add($"C{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x:F1},{y:F1}"); return this; }
    public PathBuilder Z() { _parts.Add("Z"); return this; }

    public override string ToString() => string.Join(" ", _parts);
}
