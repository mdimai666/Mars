using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace.ActionManager;

public readonly struct Hotkey
{
    public ModCode Modifiers { get; }
    public Code Code { get; }

    public Hotkey(Code code)
    {
        Modifiers = ModCode.None;
        Code = code;
    }

    public Hotkey(ModCode modifiers, Code code)
    {
        Modifiers = modifiers;
        Code = code;
    }

    public override string ToString()
    {
        var modifiers = Modifiers;
        var mods = modifiers == ModCode.None
            ? ""
            : string.Join("+", Enum.GetValues<ModCode>()
                                   .Where(m => m != ModCode.None && modifiers.HasFlag(m))
                                   .Select(m => m.ToString()));

        return string.IsNullOrEmpty(mods) ? Code.ToString() : $"{mods}+{Code}";
    }

    // --- implicit/explicit conversions ---
    public static implicit operator string(Hotkey hotkey) => hotkey.ToString();

    public static explicit operator Hotkey(string str) => Parse(str);

    // --- Parsing ---
    public static Hotkey Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            throw new ArgumentException("Hotkey string is empty", nameof(str));

        var parts = str.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        ModCode mods = ModCode.None;
        string? code = null;

        foreach (var part in parts)
        {
            if (Enum.TryParse<ModCode>(part, ignoreCase: true, out var m))
                mods |= m;
            else
                code = part;
        }

        if (code == null)
            throw new FormatException($"Hotkey string does not contain code: {str}");

        return new Hotkey(mods, new Code(code));
    }

    public static bool TryParse(string str, out Hotkey hotkey)
    {
        try
        {
            hotkey = Parse(str);
            return true;
        }
        catch
        {
            hotkey = default;
            return false;
        }
    }

    public override bool Equals(object? obj) =>
        obj is Hotkey hk && hk.Modifiers == Modifiers && hk.Code.Equals(Code);

    public override int GetHashCode() => HashCode.Combine(Modifiers, Code);
}
