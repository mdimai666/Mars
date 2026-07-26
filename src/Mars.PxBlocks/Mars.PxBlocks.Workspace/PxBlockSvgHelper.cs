using Mars.PxBlocks.Shared;

namespace Mars.PxBlocks.Workspace;

/// <summary>
/// SVG path generator matching MakeCode / Blockly block shapes.
/// Corner radius = 4. Tab/notch = standard Blockly proportions.
/// </summary>
public static class PxBlockSvgHelper
{
    public const float R = 4f;
    public const float RowH = 32f;
    public const float RowPad = 4f;
    public const float TabH = 20f;
    public const float TabW = 8f;
    public const float NotchH = 4f;
    public const float StatementIndent = 32f;
    public const float ValueH = 48f;
    public const float ValueConnR = 24f;
    public const float FieldPadX = 8f;
    public const float MinBlockW = 60f;

    public const float DomeH = R * 3; // 12f — hat dome height (before corner radius)
    public const float DomeBodyTop = DomeH + R; // 16f — hat body top Y

    // --- Measurement ---

    public static float BodyH(int inputCount) =>
        NotchH + R + RowPad * 2 + RowH + inputCount * (RowH + RowPad * 2);

    public static float TotalH(float bodyH) => bodyH + TabH;

    public static float StatementWidth(float totalFieldW) =>
        Math.Max(MinBlockW, totalFieldW + FieldPadX * 2);

    public static float ValueWidth(float totalFieldW) =>
        Math.Max(40f, totalFieldW + FieldPadX + ValueConnR) + ValueConnR;

    public static float EstimateFieldWidth(PxField f) => f switch
    {
        PxLabelField l => l.Text.Length * 7f + 6,
        PxTextField t => Math.Max(40f, (t.Value.Length > 0 ? t.Value.Length : (t.Placeholder?.Length ?? 3)) * 7.5f + 12),
        PxNumberField n => n.Value.ToString().Length * 7.5f + 14,
        PxDropdownField d => d.SelectedValue.Length * 7f + 18,
        PxCheckboxField _ => 24f,
        PxVariableField v => v.VariableName.Length * 7f + 12,
        _ => 40f
    };

    public static float TotalFieldWidth(List<PxField> fields)
    {
        float w = 0;
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) w += 4;
            w += EstimateFieldWidth(fields[i]);
        }
        return w;
    }

    public static float ValueSlotX(float blockWidth) => blockWidth - ValueConnR - 8;

    /// <summary>Label Y for statement blocks.</summary>
    public static float LabelYStmt => NotchH + R + RowPad + RowH / 2;

    /// <summary>Label Y for hat blocks (body starts at DomeBodyTop).</summary>
    public static float LabelYHat => DomeBodyTop + RowPad + RowH / 2;

    /// <summary>Y of first input row for statement blocks.</summary>
    public static float InputRowTopStmt(int index) =>
        NotchH + R + RowPad * 2 + RowH + index * (RowH + RowPad * 2);

    /// <summary>Y of first input row for hat blocks.</summary>
    public static float InputRowTopHat(int index) =>
        DomeBodyTop + RowPad * 2 + RowH + index * (RowH + RowPad * 2);

    // --- Statement block path ---

    public static string StatementPath(float width, float bodyH, bool hasStatementInput)
    {
        float si = hasStatementInput ? StatementIndent : 0;
        float cx = width / 2;
        float yEnd = bodyH;
        float notchL = cx - TabW;
        float notchR = cx + TabW;

        var p = new PathBuilder();

        // Start at (0, NotchH+R) — top-left after notch indent
        p.M(0, NotchH + R);
        // Top-left rounded corner going UP
        p.A(R, R, 0, 0, 1, R, NotchH);
        // Top edge left of notch
        p.L(notchL, NotchH);
        // Notch indent
        p.L(notchL, 0);
        p.L(notchR, 0);
        p.L(notchR, NotchH);
        // Top edge right
        p.L(width - R, NotchH);
        // Top-right corner
        p.A(R, R, 0, 0, 1, width, NotchH + R);

        // Right edge down. Label row covers y=NotchH+R to y=NotchH+R+RowPad*2+RowH.
        float labelEnd = NotchH + R + RowPad * 2 + RowH;

        if (hasStatementInput)
        {
            p.V(labelEnd);
            // C-gap
            float gapTop = labelEnd;
            float gapBottom = yEnd - RowPad;
            p.V(gapTop);
            p.L(width - si, gapTop);
            p.L(width - si, gapBottom);
            p.L(width, gapBottom);
            p.V(yEnd - R);
        }
        else
        {
            // No inputs — right edge goes straight to bottom corner
            p.V(yEnd - R);
        }

        // Bottom-right corner
        p.A(R, R, 0, 0, 1, width - R, yEnd);
        // Bottom to tab
        p.L(notchR, yEnd);
        p.L(notchR, yEnd + TabH);
        p.L(notchL, yEnd + TabH);
        p.L(notchL, yEnd);
        p.L(R, yEnd);
        // Bottom-left corner
        p.A(R, R, 0, 0, 1, 0, yEnd - R);

        // Left edge up
        if (hasStatementInput)
        {
            float gapBottom = yEnd - RowPad;
            float gapTop = labelEnd;
            p.L(0, gapBottom);
            p.L(si, gapBottom);
            p.L(si, gapTop);
            p.L(0, gapTop);
        }

        p.L(0, NotchH + R);
        p.Z();
        return p.ToString();
    }

    /// <summary>Connection indicator tab rendered inside the C-gap interior.
    /// Width w = blockWidth - 2*StatementIndent. Notch at bottom center for child-block tab.</summary>
    public static string StatementConnectorPath(float w)
    {
        if (w < TabW * 4) w = TabW * 4;
        float cx = w / 2;
        float H = RowH * 0.4f;
        float nd = R * 2;
        float nw = TabW;

        var p = new PathBuilder();
        p.M(R, 0);
        p.L(w - R, 0);
        p.A(R, R, 0, 0, 1, w, R);
        p.L(w, H);
        p.L(cx + nw, H);
        p.L(cx + nw, H - nd);
        p.L(cx - nw, H - nd);
        p.L(cx - nw, H);
        p.L(R, H);
        p.A(R, R, 0, 0, 1, 0, H - R);
        p.L(0, R);
        p.A(R, R, 0, 0, 1, R, 0);
        p.Z();
        return p.ToString();
    }

    // --- Hat block path ---

    public static string HatPath(float width, float bodyH, bool hasStatementInput)
    {
        float si = hasStatementInput ? StatementIndent : 0;
        float cx = width / 2;
        float yEnd = bodyH;

        var p = new PathBuilder();
        p.M(cx, 0);
        // Left dome
        p.C(cx - DomeH * 2, 0, 0, DomeH, 0, DomeBodyTop);
        // Left edge down
        if (hasStatementInput)
        {
            float gapTop = DomeBodyTop + RowPad * 2 + RowH;
            float gapBottom = yEnd - RowPad;
            p.L(0, gapTop);
            p.L(si, gapTop);
            p.L(si, gapBottom);
            p.L(0, gapBottom);
        }
        p.L(0, yEnd - R);
        // Bottom-left corner
        p.A(R, R, 0, 0, 1, R, yEnd);
        // Bottom to tab
        p.L(cx - TabW, yEnd);
        p.L(cx - TabW, yEnd + TabH);
        p.L(cx + TabW, yEnd + TabH);
        p.L(cx + TabW, yEnd);
        // Bottom right
        p.L(width - R, yEnd);
        p.A(R, R, 0, 0, 1, width, yEnd - R);
        // Right edge up
        if (hasStatementInput)
        {
            float gapTop = DomeBodyTop + RowPad * 2 + RowH;
            float gapBottom = yEnd - RowPad;
            p.L(width, gapBottom);
            p.L(width - si, gapBottom);
            p.L(width - si, gapTop);
            p.L(width, gapTop);
        }
        p.L(width, DomeBodyTop);
        // Right dome
        p.C(width, DomeH, cx + DomeH * 2, 0, cx, 0);
        p.Z();
        return p.ToString();
    }

    // --- Value block path ---

    /// <summary>Value/reporter block — puzzle-piece hexagon.</summary>
    public static string ValuePath(float width)
    {
        float vo = ValueConnR;
        float hh = ValueH / 2;
        var p = new PathBuilder();
        // Start at connector tip
        p.M(-vo, hh);
        // Top-left slope
        p.L(0, 0);
        // Top edge
        p.L(width - vo, 0);
        // Top-right rounded
        p.A(vo, vo, 0, 0, 1, width, hh);
        // Bottom-right rounded
        p.A(vo, vo, 0, 0, 1, width - vo, ValueH);
        // Bottom edge
        p.L(0, ValueH);
        // Bottom-left slope
        p.L(-vo, hh);
        p.Z();
        return p.ToString();
    }

    /// <summary>Value input connector — capsule shape.</summary>
    public static string ValueConnectorPath(float fieldWidth)
    {
        float r = 16f;
        float w = Math.Max(fieldWidth, r * 2);
        var p = new PathBuilder();
        p.M(r, -r);
        p.L(w - r, -r);
        p.A(r, r, 0, 0, 1, w, 0);
        p.A(r, r, 0, 0, 1, w - r, r);
        p.L(r, r);
        p.A(r, r, 0, 0, 1, 0, 0);
        p.A(r, r, 0, 0, 1, r, -r);
        p.Z();
        return p.ToString();
    }

    // --- Shadow / Highlight ---

    public static string ShadowPath(float width, float bodyH)
    {
        float sh = 3f;
        float y = bodyH - sh;
        return FormattableString.Invariant($"M0,{y:F1} L{width:F1},{y:F1} L{width:F1},{bodyH:F1} L0,{bodyH:F1} Z");
    }

    public static string HighlightPath(float width)
    {
        float hh = 3f;
        var p = new PathBuilder();
        p.M(R, 0);
        p.L(width - R, 0);
        p.A(R, R, 0, 0, 1, width, R);
        p.L(width, R + hh);
        p.L(0, R + hh);
        p.L(0, R);
        p.A(R, R, 0, 0, 1, R, 0);
        p.Z();
        return p.ToString();
    }
}

internal class PathBuilder
{
    private static readonly IFormatProvider Inv = System.Globalization.CultureInfo.InvariantCulture;
    private readonly List<string> _p = [];

    public PathBuilder M(float x, float y) { _p.Add(FormattableString.Invariant($"M{x:F1},{y:F1}")); return this; }
    public PathBuilder L(float x, float y) { _p.Add(FormattableString.Invariant($"L{x:F1},{y:F1}")); return this; }
    public PathBuilder V(float y) { _p.Add(FormattableString.Invariant($"V{y:F1}")); return this; }
    public PathBuilder H(float x) { _p.Add(FormattableString.Invariant($"H{x:F1}")); return this; }
    public PathBuilder A(float rx, float ry, float rot, int large, int sweep, float x, float y)
    { _p.Add(FormattableString.Invariant($"A{rx:F1},{ry:F1} {rot} {large},{sweep} {x:F1},{y:F1}")); return this; }
    public PathBuilder C(float x1, float y1, float x2, float y2, float x, float y)
    { _p.Add(FormattableString.Invariant($"C{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x:F1},{y:F1}")); return this; }
    public PathBuilder Z() { _p.Add("Z"); return this; }
    public override string ToString() => string.Join(" ", _p);
}
