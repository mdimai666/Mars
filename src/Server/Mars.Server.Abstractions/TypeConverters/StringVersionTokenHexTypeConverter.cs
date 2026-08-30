using System.ComponentModel;
using System.Globalization;
using Mars.Server.Abstractions.Models;

namespace Mars.Server.Abstractions.TypeConverters;

public class StringVersionTokenHexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        var casted = value as string;
        return casted != null
            ? VersionTokenHex.FromString(casted)
            : base.ConvertFrom(context, culture, value);
    }
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        var casted = value as VersionTokenHex;
        return destinationType == typeof(string) && casted is not null
            ? casted.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
    }
}
