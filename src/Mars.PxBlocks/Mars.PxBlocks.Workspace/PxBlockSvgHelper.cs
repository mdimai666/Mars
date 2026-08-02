using Mars.PxBlocks.Shared;

namespace Mars.PxBlocks.Workspace;

public static class PxBlockSvgHelper
{
    public const float GRID = 4f;
    public const float CORNER = GRID;
    public const float NOTCH_W = 9 * GRID;
    public const float NOTCH_H = 2 * GRID;
    public const float ROW_H = 12 * GRID;
    public const float MEDIUM_PAD = 2 * GRID;
    public const float LARGE_PAD = 4 * GRID;

    // C-shape for statement inputs
    public const float C_INDENT = 16 * GRID; // 64px - отступ C-gap от левого края
    public const float C_NOTCH_W = NOTCH_W;
    public const float C_NOTCH_H = NOTCH_H;

    public const float START_HAT_H = 22f;
    public const float START_HAT_W = 96f;
    public const float START_HAT_RENDER_H = START_HAT_H * 0.75f;

    public const float VALUE_H = 10 * GRID; // 40px - высота value блока
    public const float VALUE_MAX_W = 12 * GRID;

    // === Measurements ===

    public static float BodyH(int stmtCount) =>
        NOTCH_H + ROW_H + stmtCount * ROW_H;

    public static float TotalH(float bodyH) => bodyH + NOTCH_H;

    public static float BlockTotalH(PxBlock b) =>
        b is PxBlockValue ? VALUE_H : TotalH(BodyH(CountStmtInputs(b.Inputs)));

    public static float StmtWidth(float fieldW) =>
        Math.Max(120f, fieldW + MEDIUM_PAD * 2 + NOTCH_W);

    public static float ValWidth(float fieldW) =>
        Math.Max(80f, fieldW + MEDIUM_PAD * 2);

    public static int CountStmtInputs(List<PxInput> inputs) =>
        inputs.Count(i => i.Type == PxInputType.Statement);

    public static float FieldW(PxField f) => f switch
    {
        PxLabelField l => l.Text.Length * 8f + 8,
        PxTextField t => Math.Max(40f, (t.Value.Length > 0 ? t.Value.Length : 3) * 7f + 12),
        PxNumberField n => Math.Max(28f, n.Value.ToString().Length * 7f + 12),
        PxDropdownField d => Math.Max(40f, d.SelectedValue.Length * 7f + 20),
        PxCheckboxField _ => 24f,
        PxVariableField v => Math.Max(36f, v.VariableName.Length * 7f + 12),
        _ => 40f
    };

    public static float TotalFieldW(List<PxField> fields)
    {
        float w = 0;
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) w += 6;
            w += FieldW(fields[i]);
        }
        return w;
    }

    // Y positions
    public static float LabelY(bool isHat) =>
        isHat ? START_HAT_RENDER_H + ROW_H / 2 : NOTCH_H + ROW_H / 2;

    public static float StmtInputY(int index, bool isHat) =>
        (isHat ? START_HAT_RENDER_H : NOTCH_H) + ROW_H + index * ROW_H;

    // Value input position (inline after fields)
    public static float ValInputX(float blockWidth, float totalFieldW) =>
        MEDIUM_PAD + totalFieldW + 6;

    public static float ValInputY(bool isHat) => LabelY(isHat);

    // === SVG Paths ===

    public static string NotchRight() =>
        "c 2,0 3,1 4,2 l 4,4 c 1,1 2,2 4,2 h 12 c 2,0 3,-1 4,-2 l 4,-4 c 1,-1 2,-2 4,-2";

    public static string NotchLeft() =>
        "c -2,0 -3,1 -4,2 l -4,4 c -1,1 -2,2 -4,2 h -12 c -2,0 -3,-1 -4,-2 l -4,-4 c -1,-1 -2,-2 -4,-2";

    public static string HatTop() => "c 25,-22 71,-22 96,0";

    public static string CornerTL() => "M 0,4 a 4,4 0 0,1 4,-4";
    public static string CornerTR() => "a 4,4 0 0,1 4,4";
    public static string CornerBR() => "a 4,4 0 0,1 -4,4";
    public static string CornerBL() => "a 4,4 0 0,1 -4,-4";
    public static string InsideCornerTop() => "a 4,4 0 0,0 -4,4";
    public static string InsideCornerBot() => "a 4,4 0 0,0 4,4";

    // Statement block with C-gap
    public static string StmtPath(float w, float bodyH, bool hasC)
    {
        float cx = w / 2;
        float notchX = cx - NOTCH_W / 2;
        float yEnd = bodyH;

        var p = new System.Text.StringBuilder();
        p.Append($"M 0,{CORNER} ");
        p.Append(CornerTL()).Append(' ');
        p.Append($"H {notchX} ");
        p.Append(NotchRight()).Append(' ');
        p.Append($"H {w - CORNER} ");
        p.Append(CornerTR()).Append(' ');

        if (hasC)
        {
            float cTop = NOTCH_H + ROW_H;
            float cBot = yEnd;
            float cInsideH = cBot - cTop - NOTCH_H - 2 * CORNER;

            p.Append($"V {cTop} ");
            p.Append($"H {C_INDENT} ");
            p.Append(NotchLeft()).Append(' ');
            p.Append($"h -{C_INDENT - NOTCH_W - CORNER} ");
            p.Append(InsideCornerTop()).Append(' ');
            p.Append($"v {cInsideH} ");
            p.Append(InsideCornerBot()).Append(' ');
            p.Append($"h {C_INDENT - NOTCH_W - CORNER} ");
            p.Append($"H {w} ");
            p.Append($"V {yEnd - CORNER} ");
        }
        else
        {
            p.Append($"V {yEnd - CORNER} ");
        }

        p.Append(CornerBR()).Append(' ');
        float tabRight = cx + NOTCH_W / 2;
        p.Append($"H {tabRight} ");
        p.Append(NotchLeft()).Append(' ');
        p.Append($"H {CORNER} ");
        p.Append(CornerBL()).Append(' ');
        p.Append($"V {CORNER} ");
        p.Append("Z");
        return p.ToString();
    }

    // Hat block with C-gap
    public static string HatPath(float w, float bodyH, bool hasC)
    {
        float cx = w / 2;
        float yEnd = bodyH;
        float hatX = cx - START_HAT_W / 2;

        var p = new System.Text.StringBuilder();
        p.Append($"M {hatX},0 ");
        p.Append(HatTop()).Append(' ');
        p.Append($"H {w - CORNER} ");
        p.Append(CornerTR()).Append(' ');

        if (hasC)
        {
            float cTop = START_HAT_RENDER_H + ROW_H;
            float cBot = yEnd;
            float cInsideH = cBot - cTop - NOTCH_H - 2 * CORNER;

            p.Append($"V {cTop} ");
            p.Append($"H {C_INDENT} ");
            p.Append(NotchLeft()).Append(' ');
            p.Append($"h -{C_INDENT - NOTCH_W - CORNER} ");
            p.Append(InsideCornerTop()).Append(' ');
            p.Append($"v {cInsideH} ");
            p.Append(InsideCornerBot()).Append(' ');
            p.Append($"h {C_INDENT - NOTCH_W - CORNER} ");
            p.Append($"H {w} ");
            p.Append($"V {yEnd - CORNER} ");
        }
        else
        {
            p.Append($"V {yEnd - CORNER} ");
        }

        p.Append(CornerBR()).Append(' ');
        float tabRight = cx + NOTCH_W / 2;
        p.Append($"H {tabRight} ");
        p.Append(NotchLeft()).Append(' ');
        p.Append($"H {CORNER} ");
        p.Append(CornerBL()).Append(' ');
        p.Append($"V 0 ");
        p.Append($"H {hatX} Z");
        return p.ToString();
    }

    // Value block (rounded rectangle)
    public static string ValPath(float w)
    {
        float h = VALUE_H;
        float r = CORNER; // 4px corners

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

    // C-gap area for statement input (where child blocks render)
    public static float CGapX() => C_INDENT;
    public static float CGapY(int index, bool isHat) => StmtInputY(index, isHat);
    public static float CGapW(float blockW) => blockW - C_INDENT;
    public static float CGapH(int totalStmtInputs, int currentIndex) => ROW_H;
}
