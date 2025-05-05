using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mars.Host.Shared.JsonConverters;
using Mars.Host.Shared.TypeConverters;

namespace Mars.Host.Shared.Models;

[JsonConverter(typeof(VersionTokenHexJsonConverter))]
[TypeConverter(typeof(StringVersionTokenHexTypeConverter))]
public class VersionTokenHex
{
    readonly uint entityVersion;

    public uint EntityVersion => entityVersion;

    public VersionTokenHex(uint entityVersion)
    {
        this.entityVersion = entityVersion;
    }

    public override string ToString()
    {
        return ConvertToHexString(entityVersion); ;
    }

    string ConvertToHexString(uint entityVersion)
    {
        return entityVersion.ToString("X");
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null) return false;

        if (obj is VersionTokenHex vt)
        {
            return vt.entityVersion.Equals(entityVersion);
        }
        else if (obj is string str)
        {
            return ConvertToHexString(entityVersion).Equals(str);
        }
        return base.Equals(obj);
    }

    public bool Equals(VersionTokenHex val)
    {
        return entityVersion.Equals(val);
    }

    public static VersionTokenHex FromString(string hexStringVersion)
    {
        try
        {
            var value = Convert.ToUInt32(hexStringVersion, 16);
            return new VersionTokenHex(value);
        }
        catch (FormatException)
        {
            throw new ValidationException($"value must be Hex string. But recived '{hexStringVersion}'");
        }
    }

    public static implicit operator VersionTokenHex(string value)
    {
        return FromString(value);
    }

    public static implicit operator VersionTokenHex(uint value)
    {
        return new VersionTokenHex(value);
    }

    public static implicit operator string(VersionTokenHex value)
    {
        return value.ToString();
    }

    public static implicit operator uint(VersionTokenHex value)
    {
        return value.entityVersion;
    }

    public static bool operator ==(VersionTokenHex left, VersionTokenHex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VersionTokenHex left, VersionTokenHex right)
    {
        return !(left == right);
    }

    public override int GetHashCode() => entityVersion.GetHashCode();
}
