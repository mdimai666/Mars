using PhoneNumbers;

namespace Mars.Host.Shared.Utils;

public static class PhoneUtil
{
    public static string NormalizePhone(string phone)
    {
        ArgumentException.ThrowIfNullOrEmpty(phone, nameof(phone));
        var phoneUtil = PhoneNumberUtil.GetInstance();

        var parsed = phoneUtil.Parse(phone, null);
        return phoneUtil.Format(parsed, PhoneNumberFormat.E164); // результат вида +79161234567
    }

    public static bool TryNormalizePhone(string? phone, out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(phone))
            return false;

        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var parsed = phoneUtil.Parse(phone, null);

            if (!phoneUtil.IsValidNumber(parsed))
                return false;

            normalized = phoneUtil.Format(parsed, PhoneNumberFormat.E164);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? GetNormalizedOrNull(string? phone)
    {
        return TryNormalizePhone(phone, out var normalized) ? normalized : null;
    }
}
