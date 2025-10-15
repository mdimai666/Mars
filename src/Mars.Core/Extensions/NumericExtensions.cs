namespace Mars.Core.Extensions;

public static class NumericExtensions
{
    /// <summary>
    /// Округляет число до заданного количества знаков после запятой.
    /// </summary>
    /// <param name="decimalPlaces">чисел после точки</param>
    public static double Round(this double value, int decimalPlaces = 2) => Math.Round(value, decimalPlaces);

    /// <summary>
    /// Отсекает дробную часть числа после указанного количества знаков.
    /// </summary>
    /// <param name="digits">чисел после точки</param>
    /// <returns>digits = 2 — 58.9876 → 58.98.</returns>
    public static double TruncateFractional(this double value, int digits)
    {
        var trunc = Math.Pow(10, digits);
        return Math.Truncate(value * trunc) / trunc;
    }

    /// <summary>
    /// Округляет число до заданного количества знаков после запятой.
    /// </summary>
    /// <param name="decimalPlaces">чисел после точки</param>
    public static decimal Round(this decimal value, int decimalPlaces = 2) => Math.Round(value, decimalPlaces);

    /// <summary>
    /// Округляет число до заданного количества знаков после запятой.
    /// </summary>
    public static decimal MoneyRound(this decimal value) => Math.Round(value, 2);

    /// <summary>
    /// Отсекает дробную часть числа после указанного количества знаков.
    /// </summary>
    /// <param name="digits">чисел после точки</param>
    /// <returns>digits = 2 — 58.9876 → 58.98.</returns>
    public static decimal TruncateFractional(this decimal value, int digits)
    {
        var trunc = (decimal)Math.Pow(10, digits);
        return Math.Truncate(value * trunc) / trunc;
    }
}
