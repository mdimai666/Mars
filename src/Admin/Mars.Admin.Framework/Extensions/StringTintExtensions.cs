namespace Mars.Admin.Framework.Extensions;

/// <summary>
/// Детерминированный тинт (0..mod-1) для строки. string.GetHashCode() рандомизирован
/// на процесс — цвета «прыгали» бы при каждой перезагрузке приложения.
/// </summary>
public static class StringTintExtensions
{
    public static int StableTint(this string s, int mod = 5)
    {
        var hash = 0;
        foreach (var c in s) hash = unchecked(hash * 31 + c);
        return (hash & 0x7FFFFFFF) % mod;
    }
}
