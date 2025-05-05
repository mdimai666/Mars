using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Mars.Shared.DocumentHelpers.Snils;

internal static class ValidationsHelper
{
    internal const byte NonDigitByte = 255;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte GetNumericValue(in char c) =>
        c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            _ => NonDigitByte
        };
}

public class ValidateSnils
{
    private const byte NonDigitByte = 255;

    private static readonly byte[] _minimalCheckableNumberDigits = { 0, 0, 1, 0, 0, 1, 9, 9, 8 };

    /// <summary>
    ///     Determines whether the specified Snils string is valid
    /// </summary>
    /// <param name = "snilsString"> The Snils string </param>
    /// <returns>
    ///     <c> true </c> if is valid ; otherwise, <c> false </c>.
    /// </returns>
    /// <remarks>
    ///     Valid Snils string consists of 11 numbers
    ///     (https://ru.wikipedia.org/wiki/Контрольное_число#Страховой_номер_индивидуального_лицевого_счёта_(Россия))
    /// </remarks>
    [Pure]
    //[PublicAPI]
    //[ContractAnnotation("null => false")]
    public static bool IsValidSnils([AllowNull, NotNullWhen(true)] string snilsString)
    {
        if (snilsString is null || snilsString.Length != 11)
        {
            return false;
        }

        var sum = 0;
        var noNeedToCheckSum = true;

        for (var index = 0; index < 9; index++)
        {
            var current = ValidationsHelper.GetNumericValue(snilsString[index]);

            if (current == NonDigitByte)
            {
                return false;
            }

            noNeedToCheckSum &= current <= _minimalCheckableNumberDigits[index];

            sum += current * (9 - index);
        }

        if (noNeedToCheckSum)
        {
            return true;
        }

        var tenth = ValidationsHelper.GetNumericValue(snilsString[9]);

        if (tenth == NonDigitByte)
        {
            return false;
        }

        var eleventh = ValidationsHelper.GetNumericValue(snilsString[10]);

        if (eleventh == NonDigitByte)
        {
            return false;
        }

        var controlSum = tenth * 10 + eleventh;

        if (sum < 100)
        {
            return controlSum == sum;
        }

        if (sum == 100 || sum == 101)
        {
            return controlSum == 0;
        }

        sum %= 101;

        if (sum != 100)
        {
            return controlSum == sum;
        }

        return controlSum == 0;
    }
}
