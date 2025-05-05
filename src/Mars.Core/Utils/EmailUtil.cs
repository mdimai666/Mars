using System.Text.RegularExpressions;

namespace Mars.Core.Utils;

public static class EmailUtil
{
    private static readonly Regex EmailRegex = new Regex("\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*", RegexOptions.IgnoreCase);
    public static bool IsEmail(string? input)
        => string.IsNullOrEmpty(input) ? false : EmailRegex.Match(input).Success;
}
