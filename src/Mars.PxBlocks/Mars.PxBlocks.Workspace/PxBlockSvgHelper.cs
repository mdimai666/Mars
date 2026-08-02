using Mars.PxBlocks.Shared;

namespace Mars.PxBlocks.Workspace;

/// <summary>
/// SVG path generator matching Blockly Zelos renderer (used by PXT/MakeCode).
/// All constants and paths extracted from Blockly source (GRID_UNIT=4).
/// </summary>
public static class PxBlockSvgHelper
{
    // === Zelos Constants (GRID_UNIT = 4) ===
    public const float GRID_UNIT = 4f;

    public const float CORNER_RADIUS = 1 * GRID_UNIT;           // 4
    public const float NOTCH_WIDTH = 9 * GRID_UNIT;             // 36
    public const float NOTCH_HEIGHT = 2 * GRID_UNIT;            // 8
    public const float NOTCH_OFFSET_LEFT = 3 * GRID_UNIT;       // 12
    public const float STATEMENT_INPUT_NOTCH_OFFSET = 4 * GRID_UNIT; // 16 (NOTCH_OFFSET_LEFT + INSIDE_CORNERS.rightWidth)
    public const float STATEMENT_INPUT_PADDING_LEFT = 4 * GRID_UNIT; // 16
    public const float MIN_BLOCK_HEIGHT = 12 * GRID_UNIT;       // 48
    public const float EMPTY_INLINE_INPUT_HEIGHT = 8 * GRID_UNIT; // 32
    public const float EMPTY_INLINE_INPUT_PADDING = 4 * GRID_UNIT; // 16
    public const float LARGE_PADDING = 4 * GRID_UNIT;           // 16
    public const float MEDIUM_PADDING = 2 * GRID_UNIT;          // 8
    public const float MEDIUM_LARGE_PADDING = 3 * GRID_UNIT;    // 12
    public const float SMALL_PADDING = 1 * GRID_UNIT;           // 4

    public const float START_HAT_HEIGHT = 22f;
    public const float START_HAT_WIDTH = 96f;
    public const float START_HAT_RENDERED_HEIGHT = START_HAT_HEIGHT * 0.75f; // 16.5

    public const float MIN_BLOCK_WIDTH = 2 * GRID_UNIT;         // 8
    public const float FIELD_TEXT_FONTSIZE = 3 * GRID_UNIT;     // 12

    // Value input shapes
    public const float MAX_DYNAMIC_SHAPE_WIDTH = 12 * GRID_UNIT; // 48
    public const float VALUE_INPUT_HEIGHT = EMPTY_INLINE_INPUT_HEIGHT; // 32

    // Derived
    public const float INSIDE_CORNER_RADIUS = CORNER_RADIUS;    // 4
    public const float INSIDE_CORNER_WIDTH = CORNER_RADIUS;     // 4
    public const float INSIDE_CORNER_HEIGHT = CORNER_RADIUS;    // 4

    // Row height for statement blocks
    public const float ROW_HEIGHT = MIN_BLOCK_HEIGHT;           // 48 (one row = min block height)
    public const float LABEL_ROW_HEIGHT = LARGE_PADDING + NOTCH_HEIGHT; // 24 (top row with notch)

    // === Measurement ===

    /// <summary>Block body height. Only statement inputs add extra rows.</summary>
    public static float BodyH(int statementInputCount) =>
        NOTCH_HEIGHT + ROW_HEIGHT + statementInputCount * ROW_HEIGHT;

    public static float TotalH(float bodyH) => bodyH + NOTCH_HEIGHT;

    public static float StatementWidth(float totalFieldW) =>
        Math.Max(80f, totalFieldW + MEDIUM_PADDING * 2 + NOTCH_WIDTH);

    public static float ValueWidth(float totalFieldW) =>
        Math.Max(60f, totalFieldW + MEDIUM_PADDING * 2);

    public static float EstimateFieldWidth(PxField f) => f switch
    {
        PxLabelField l => l.Text.Length * 8f + 8,
        PxTextField t => Math.Max(48f, (t.Value.Length > 0 ? t.Value.Length : (t.Placeholder?.Length ?? 3)) * 8f + 16),
        PxNumberField n => n.Value.ToString().Length * 8f + 16,
        PxDropdownField d => d.SelectedValue.Length * 8f + 24,
        PxCheckboxField _ => 28f,
        PxVariableField v => v.VariableName.Length * 8f + 16,
        _ => 48f
    };

    public static float TotalFieldWidth(List<PxField> fields)
    {
        float w = 0;
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) w += 6;
            w += EstimateFieldWidth(fields[i]);
        }
        return w;
    }

    public static float ValueSlotX(float blockWidth) => blockWidth - MAX_DYNAMIC_SHAPE_WIDTH - 8;

    /// <summary>Label Y for statement blocks (center of first row).</summary>
    public static float LabelYStmt => NOTCH_HEIGHT + ROW_HEIGHT / 2;

    /// <summary>Label Y for hat blocks.</summary>
    public static float LabelYHat => START_HAT_RENDERED_HEIGHT + ROW_HEIGHT / 2;

    /// <summary>Y of Nth statement input row (0-based).</summary>
    public static float StatementInputRowTop(int index) =>
        NOTCH_HEIGHT + ROW_HEIGHT + index * ROW_HEIGHT;

    /// <summary>Y of Nth statement input row for hat blocks.</summary>
    public static float StatementInputRowTopHat(int index) =>
        START_HAT_RENDERED_HEIGHT + ROW_HEIGHT + index * ROW_HEIGHT;

    public static int CountStatementInputs(List<PxInput> inputs) =>
        inputs.Count(i => i.Type == PxInputType.Statement);

    // === Notch Path (Zelos, NOTCH_WIDTH=36, NOTCH_HEIGHT=8) ===

    /// <summary>
    /// Notch path going RIGHT (left side to right side, curving DOWN into block).
    /// This is the "previous connection" notch at the top of a block.
    /// </summary>
    public static string NotchPathRight()
    {
        // c 2,0 3,1 4,2  l 4,4  c 1,1 2,2 4,2  h 12  c 2,0 3,-1 4,-2  l 4,-4  c 1,-1 2,-2 4,-2
        return "c 2,0 3,1 4,2 l 4,4 c 1,1 2,2 4,2 h 12 c 2,0 3,-1 4,-2 l 4,-4 c 1,-1 2,-2 4,-2";
    }

    /// <summary>
    /// Notch path going LEFT (right side to left side, mirror of NotchPathRight).
    /// This is the "next connection" tab at the bottom of a block.
    /// </summary>
    public static string NotchPathLeft()
    {
        // c -2,0 -3,1 -4,2  l -4,4  c -1,1 -2,2 -4,2  h -12  c -2,0 -3,-1 -4,-2  l -4,-4  c -1,-1 -2,-2 -4,-2
        return "c -2,0 -3,1 -4,2 l -4,4 c -1,1 -2,2 -4,2 h -12 c -2,0 -3,-1 -4,-2 l -4,-4 c -1,-1 -2,-2 -4,-2";
    }

    // === Hat Path ===

    /// <summary>Hat block top curve (single cubic bezier).</summary>
    public static string HatPathTop()
    {
        // c 25,-22 71,-22 96,0
        return "c 25,-22 71,-22 96,0";
    }

    // === Outside Corners (CORNER_RADIUS=4) ===

    public static string OutsideCornerTopLeft() => "M 0,4 a 4,4 0 0,1 4,-4";
    public static string OutsideCornerTopRight() => "a 4,4 0 0,1 4,4";
    public static string OutsideCornerBottomRight() => "a 4,4 0 0,1 -4,4";
    public static string OutsideCornerBottomLeft() => "a 4,4 0 0,1 -4,-4";

    // === Inside Corners (C-shape, CORNER_RADIUS=4) ===

    public static string InsideCornerTop() => "a 4,4 0 0,0 -4,4";
    public static string InsideCornerBottom() => "a 4,4 0 0,0 4,4";

    // === Statement Block Path ===

    public static string StatementPath(float width, float bodyH, bool hasStatementInput)
    {
        float cx = width / 2;
        float notchX = cx - NOTCH_WIDTH / 2; // left edge of top notch
        float yEnd = bodyH;

        var p = new System.Text.StringBuilder();

        // Start at top-left
        p.Append($"M 0,{CORNER_RADIUS} ");
        // Top-left corner
        p.Append(OutsideCornerTopLeft()).Append(' ');
        // Top edge to notch
        p.Append($"H {notchX} ");
        // Top notch (going into block)
        p.Append(NotchPathRight()).Append(' ');
        // Top edge to top-right corner
        p.Append($"H {width - CORNER_RADIUS} ");
        p.Append(OutsideCornerTopRight()).Append(' ');

        // Right edge down
        if (hasStatementInput)
        {
            float gapTop = NOTCH_HEIGHT + ROW_HEIGHT;
            float gapBottom = yEnd;
            float cIndent = STATEMENT_INPUT_NOTCH_OFFSET + NOTCH_WIDTH; // 16 + 36 = 52
            float insideHeight = gapBottom - gapTop - NOTCH_HEIGHT - 2 * INSIDE_CORNER_HEIGHT;

            p.Append($"V {gapTop} ");
            // C-shape: indent left with notch at top
            p.Append($"H {cIndent} ");
            p.Append(NotchPathLeft()).Append(' '); // notch going into C (8px deep)
            p.Append($"h -{STATEMENT_INPUT_NOTCH_OFFSET - INSIDE_CORNER_WIDTH} "); // back to inner corner
            p.Append(InsideCornerTop()).Append(' '); // round into C
            p.Append($"v {insideHeight} "); // down inside C
            p.Append(InsideCornerBottom()).Append(' '); // round out of C
            p.Append($"h {STATEMENT_INPUT_NOTCH_OFFSET - INSIDE_CORNER_WIDTH} "); // forward to notch
            // No bottom notch (connected block fills it)
            p.Append($"H {width} ");
            p.Append($"V {yEnd - CORNER_RADIUS} ");
        }
        else
        {
            p.Append($"V {yEnd - CORNER_RADIUS} ");
        }

        // Bottom-right corner
        p.Append(OutsideCornerBottomRight()).Append(' ');
        // Bottom tab (next connection)
        float tabRight = cx + NOTCH_WIDTH / 2;
        p.Append($"H {tabRight} ");
        p.Append(NotchPathLeft()).Append(' '); // tab going down
        // Bottom edge to bottom-left corner
        p.Append($"H {CORNER_RADIUS} ");
        p.Append(OutsideCornerBottomLeft()).Append(' ');

        // Left edge up (straight, no indent)
        p.Append($"V {CORNER_RADIUS} ");
        p.Append("Z");
        return p.ToString();
    }

    /// <summary>Statement connector (inside C-gap) — simple rounded rectangle.</summary>
    public static string StatementConnectorPath(float w)
    {
        if (w < 32) w = 32;
        float h = ROW_HEIGHT * 0.3f;
        float r = CORNER_RADIUS;

        var p = new System.Text.StringBuilder();
        p.Append($"M {r},0 ");
        p.Append($"H {w - r} ");
        p.Append($"a {r},{r} 0 0,1 {r},{r} ");
        p.Append($"V {h - r} ");
        p.Append($"a {r},{r} 0 0,1 -{r},{r} ");
        p.Append($"H {r} ");
        p.Append($"a {r},{r} 0 0,1 -{r},-{r} ");
        p.Append($"V {r} ");
        p.Append($"a {r},{r} 0 0,1 {r},-{r} Z");
        return p.ToString();
    }

    // === Hat Block Path ===

    public static string HatPath(float width, float bodyH, bool hasStatementInput)
    {
        float cx = width / 2;
        float yEnd = bodyH;
        float hatStartX = cx - START_HAT_WIDTH / 2;

        var p = new System.Text.StringBuilder();

        // Start at top-left of hat
        p.Append($"M {hatStartX},0 ");
        // Hat curve (ends at hatStartX + 96)
        p.Append(HatPathTop()).Append(' ');
        // Go right to right edge
        p.Append($"H {width - CORNER_RADIUS} ");
        // Top-right corner
        p.Append(OutsideCornerTopRight()).Append(' ');

        // Right edge down
        if (hasStatementInput)
        {
            float gapTop = START_HAT_RENDERED_HEIGHT + ROW_HEIGHT;
            float gapBottom = yEnd;
            float cIndent = STATEMENT_INPUT_NOTCH_OFFSET + NOTCH_WIDTH;
            float insideHeight = gapBottom - gapTop - NOTCH_HEIGHT - 2 * INSIDE_CORNER_HEIGHT;

            p.Append($"V {gapTop} ");
            // C-shape: indent left with notch at top
            p.Append($"H {cIndent} ");
            p.Append(NotchPathLeft()).Append(' ');
            p.Append($"h -{STATEMENT_INPUT_NOTCH_OFFSET - INSIDE_CORNER_WIDTH} ");
            p.Append(InsideCornerTop()).Append(' ');
            p.Append($"v {insideHeight} ");
            p.Append(InsideCornerBottom()).Append(' ');
            p.Append($"h {STATEMENT_INPUT_NOTCH_OFFSET - INSIDE_CORNER_WIDTH} ");
            p.Append($"H {width} ");
            p.Append($"V {yEnd - CORNER_RADIUS} ");
        }
        else
        {
            p.Append($"V {yEnd - CORNER_RADIUS} ");
        }

        // Bottom-right corner
        p.Append(OutsideCornerBottomRight()).Append(' ');
        // Bottom tab
        float tabRight = cx + NOTCH_WIDTH / 2;
        p.Append($"H {tabRight} ");
        p.Append(NotchPathLeft()).Append(' ');
        // Bottom edge to bottom-left corner
        p.Append($"H {CORNER_RADIUS} ");
        p.Append(OutsideCornerBottomLeft()).Append(' ');

        // Left edge up (straight)
        p.Append($"V 0 ");
        // Go right to hat start X (closes the path)
        p.Append($"H {hatStartX} Z");
        return p.ToString();
    }

    // === Value Block Path (Rounded shape) ===

    public static string ValuePath(float width)
    {
        float h = VALUE_INPUT_HEIGHT;
        float r = Math.Min(h / 2, MAX_DYNAMIC_SHAPE_WIDTH);

        var p = new System.Text.StringBuilder();
        // Start at left center
        p.Append($"M 0,{h / 2} ");
        // Top-left arc
        p.Append($"a {r},{r} 0 0,1 {r},-{r} ");
        // Top edge
        p.Append($"H {width - r} ");
        // Top-right arc
        p.Append($"a {r},{r} 0 0,1 {r},{r} ");
        // Right edge (if height > 2*r)
        if (h > 2 * r)
        {
            p.Append($"V {h - r} ");
        }
        // Bottom-right arc
        p.Append($"a {r},{r} 0 0,1 -{r},{r} ");
        // Bottom edge
        p.Append($"H {r} ");
        // Bottom-left arc
        p.Append($"a {r},{r} 0 0,1 -{r},-{r} ");
        p.Append("Z");
        return p.ToString();
    }

    /// <summary>Value input connector — rounded rectangle slot.</summary>
    public static string ValueConnectorPath(float fieldWidth)
    {
        float r = 16f;
        float w = Math.Max(fieldWidth, r * 2);
        var p = new System.Text.StringBuilder();
        p.Append($"M 0,-{r} ");
        p.Append($"H {w - r} ");
        p.Append($"a {r},{r} 0 0,1 {r},{r} ");
        p.Append($"a {r},{r} 0 0,1 -{r},{r} ");
        p.Append($"H {r} ");
        p.Append($"a {r},{r} 0 0,1 -{r},-{r} ");
        p.Append($"a {r},{r} 0 0,1 {r},-{r} Z");
        return p.ToString();
    }

    // === Shadow / Highlight ===

    public static string ShadowPath(float width, float bodyH)
    {
        float sh = 3f;
        float y = bodyH - sh;
        return $"M 0,{y:F1} L {width:F1},{y:F1} L {width:F1},{bodyH:F1} L 0,{bodyH:F1} Z";
    }

    public static string HighlightPath(float width)
    {
        float hh = 3f;
        var p = new System.Text.StringBuilder();
        p.Append($"M {CORNER_RADIUS},0 ");
        p.Append($"H {width - CORNER_RADIUS} ");
        p.Append($"a {CORNER_RADIUS},{CORNER_RADIUS} 0 0,1 {CORNER_RADIUS},{CORNER_RADIUS} ");
        p.Append($"V {CORNER_RADIUS + hh} ");
        p.Append($"H 0 ");
        p.Append($"V {CORNER_RADIUS} ");
        p.Append($"a {CORNER_RADIUS},{CORNER_RADIUS} 0 0,1 {CORNER_RADIUS},-{CORNER_RADIUS} Z");
        return p.ToString();
    }
}
