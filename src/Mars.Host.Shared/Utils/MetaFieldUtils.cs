using System.Globalization;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Utils;

public static class MetaFieldUtils
{
    public static ModifyMetaValueDetailQuery MetaValueFromJson(ModifyMetaValueDetailQuery mv, JsonValue jsonValue)
    {
        //if(mv.NULL && jsonValue == null) 

        var t = mv.MetaField.Type;
        if (t == MetaFieldType.Bool)
            return mv with { Bool = jsonValue.GetValue<bool>() };
        else if (t == MetaFieldType.Int)
            return mv with { Int = jsonValue.GetValue<int>() };
        else if (t == MetaFieldType.Float)
            return mv with { Float = jsonValue.GetValue<float>() };
        else if (t == MetaFieldType.Decimal)
            return mv with { Decimal = jsonValue.GetValue<decimal>() };
        else if (t == MetaFieldType.Long)
            return mv with { Long = jsonValue.GetValue<long>() };
        else if (t == MetaFieldType.DateTime)
            return mv with { DateTime = jsonValue.GetValue<DateTime>() };
        else if (t == MetaFieldType.String)
            return mv with { StringShort = jsonValue.GetValue<string>() };
        else if (t == MetaFieldType.Text)
            return mv with { StringText = jsonValue.GetValue<string>() };
        else if (t == MetaFieldType.Relation)
            return mv with { ModelId = jsonValue.GetValue<Guid>() };
        else if (t == MetaFieldType.Select)
            return mv with { VariantId = jsonValue.GetValue<Guid>() };
        else if (t == MetaFieldType.SelectMany)
            return mv with { VariantsIds = jsonValue.GetValue<Guid[]>() };
        else
            throw new NotImplementedException($" casting for '{t}' not implement");
    }

    public static object ConvertStringValueToMetaTypeObject(MetaFieldType t, string stringValue)
    {
        if (t == MetaFieldType.Bool)
            return bool.Parse(stringValue);
        else if (t == MetaFieldType.Int)
            return int.Parse(stringValue);
        else if (t == MetaFieldType.Float)
            return float.Parse(stringValue, CultureInfo.InvariantCulture);
        else if (t == MetaFieldType.Decimal)
            return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
        else if (t == MetaFieldType.Long)
            return long.Parse(stringValue);
        else if (t == MetaFieldType.DateTime)
            return DateTime.Parse(stringValue, CultureInfo.InvariantCulture);
        else if (t == MetaFieldType.String)
            return stringValue;
        else if (t == MetaFieldType.Text)
            return stringValue;
        else if (t == MetaFieldType.Relation)
            return Guid.Parse(stringValue);
        else if (t == MetaFieldType.Select)
            return Guid.Parse(stringValue);
        else if (t == MetaFieldType.SelectMany)
            return stringValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Guid.Parse)
                    .ToArray();
        else
            throw new NotImplementedException($"casting for '{t}' not implement");
    }

    public static ModifyMetaValueDetailQuery MetaValueFromString(ModifyMetaValueDetailQuery mv, string? stringValue)
    {
        var t = mv.MetaField.Type;

        if (string.IsNullOrWhiteSpace(stringValue))
            return mv;

        if (t == MetaFieldType.Bool)
            return mv with { Bool = bool.Parse(stringValue) };
        else if (t == MetaFieldType.Int)
            return mv with { Int = int.Parse(stringValue) };
        else if (t == MetaFieldType.Float)
            return mv with { Float = float.Parse(stringValue, CultureInfo.InvariantCulture) };
        else if (t == MetaFieldType.Decimal)
            return mv with { Decimal = decimal.Parse(stringValue, CultureInfo.InvariantCulture) };
        else if (t == MetaFieldType.Long)
            return mv with { Long = long.Parse(stringValue) };
        else if (t == MetaFieldType.DateTime)
            return mv with { DateTime = DateTime.Parse(stringValue, CultureInfo.InvariantCulture).Date };
        else if (t == MetaFieldType.String)
            return mv with { StringShort = stringValue };
        else if (t == MetaFieldType.Text)
            return mv with { StringText = stringValue };
        else if (t == MetaFieldType.Relation)
            return mv with { ModelId = Guid.Parse(stringValue) };
        else if (t == MetaFieldType.Select)
            return mv with { VariantId = Guid.Parse(stringValue) };
        else if (t == MetaFieldType.SelectMany)
            return mv with
            {
                VariantsIds = stringValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Guid.Parse)
                    .ToArray()
            };
        else
            throw new NotImplementedException($"casting for '{t}' not implement");

    }

    public static ModifyMetaValueDetailQuery TryMetaValueFromString(ModifyMetaValueDetailQuery mv, string? stringValue)
    {
        try
        {
            return MetaValueFromString(mv, stringValue);
        }
        catch (Exception ex) when (ex is not NotImplementedException)
        {
            throw new FormatException(
                $"Failed to convert value '{stringValue}' to '{mv.MetaField.Type}'",
                ex);
        }
    }

    public static ModifyMetaValueDetailQuery MetaValueFromObject(ModifyMetaValueDetailQuery mv, object value)
    {
        //if(mv.NULL && jsonValue == null) 

        var t = mv.MetaField.Type;
        if (t == MetaFieldType.Bool)
            return mv with { Bool = (bool)value };
        else if (t == MetaFieldType.Int)
            return mv with { Int = (int)value };
        else if (t == MetaFieldType.Float)
            return mv with { Float = (float)value };
        else if (t == MetaFieldType.Decimal)
            return mv with { Decimal = (decimal)value };
        else if (t == MetaFieldType.Long)
            return mv with { Long = (long)value };
        else if (t == MetaFieldType.DateTime)
            return mv with { DateTime = (DateTime)value };
        else if (t == MetaFieldType.String)
            return mv with { StringShort = (string)value };
        else if (t == MetaFieldType.Text)
            return mv with { StringText = (string)value };
        else if (t == MetaFieldType.Relation)
            return mv with { ModelId = (Guid)value };
        else if (t == MetaFieldType.Select)
            return mv with { VariantId = (Guid)value };
        else if (t == MetaFieldType.SelectMany)
            return mv with { VariantsIds = (Guid[])value };
        else
            throw new NotImplementedException($" casting for '{t}' not implement");
    }

    public static Type MetaFieldTypeToType(MetaFieldType mtype)
    {
        return mtype switch
        {
            MetaFieldType.String => typeof(string),
            MetaFieldType.Text => typeof(string),
            MetaFieldType.Bool => typeof(bool),
            MetaFieldType.Int => typeof(int),
            MetaFieldType.Long => typeof(long),
            MetaFieldType.Float => typeof(float),
            MetaFieldType.Decimal => typeof(decimal),
            MetaFieldType.DateTime => typeof(DateTime),

            MetaFieldType.Select => typeof(Guid), //typeof(MetaFieldVariant),
            MetaFieldType.SelectMany => typeof(Guid[]), //typeof(List<MetaFieldVariant>),

            MetaFieldType.Group => throw new NotImplementedException(),//todo: implement
            MetaFieldType.List => throw new NotImplementedException(),

            MetaFieldType.Relation => typeof(Guid),//IBasicEntity
            MetaFieldType.File => typeof(Guid),//FileEntity
            MetaFieldType.Image => typeof(Guid),//FileEntity

            _ => throw new NotImplementedException()
        };
    }
}
