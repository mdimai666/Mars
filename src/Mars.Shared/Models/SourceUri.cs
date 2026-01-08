using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;
using Mars.Shared.JsonConverters;

namespace Mars.Shared.Models;

/// <summary>
/// /{type}/{name}
/// EXPERIMENT
/// </summary>
[TypeConverter(typeof(SourceUriConverter))]
[JsonConverter(typeof(SourceUriJsonConverter))]
[DebuggerDisplay("{Value}")]
public class SourceUri : IEquatable<SourceUri>
{
    private static readonly SearchValues<char> s_validPathChars =
        SearchValues.Create("!$&'()*+,-./0123456789:;=@ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~");

    public const string PathMustStartWithSlashMessage = "Path must start with slash";
    public const string ValueContainNotAllowedCharsMessage = "Value '{0}' contain not allowed chars";

    public static readonly SourceUri Empty = new(string.Empty);

    readonly string[] _segments;

    public string? Root => _segments.Length > 0 ? _segments[0] : null;

    public SourceUri(string? value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '/')
        {
            throw new ArgumentException(PathMustStartWithSlashMessage);
        }
        var indexOfInvalidChar = value.AsSpan().IndexOfAnyExcept(s_validPathChars);
        if (indexOfInvalidChar > -1) throw new ArgumentException(string.Format(ValueContainNotAllowedCharsMessage, value));

        Value = value;
        _segments = HasValue ? value.Split('/', StringSplitOptions.RemoveEmptyEntries) : [];
    }

    /// <summary>
    /// The unescaped path value
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// True if the path is not empty
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue
    {
        get { return !string.IsNullOrEmpty(Value); }
    }

    public override string ToString()
    {
        return Value!;
    }

    public bool StartsWithSegments(SourceUri other)
    {
        return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase);
    }

    public bool StartsWithSegments(SourceUri other, StringComparison comparisonType)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, comparisonType))
        {
            return value1.Length == value2.Length || value1[value2.Length] == '/';
        }
        return false;
    }

    public bool StartsWithSegments(SourceUri other, out SourceUri remaining)
    {
        return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out remaining);
    }

    public bool StartsWithSegments(SourceUri other, StringComparison comparisonType, out SourceUri remaining)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, comparisonType))
        {
            if (value1.Length == value2.Length || value1[value2.Length] == '/')
            {
                remaining = new SourceUri(value1[value2.Length..]);
                return true;
            }
        }
        remaining = Empty;
        return false;
    }

    public bool StartsWithSegments(SourceUri other, out SourceUri matched, out SourceUri remaining)
    {
        return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out matched, out remaining);
    }

    public bool StartsWithSegments(SourceUri other, StringComparison comparisonType, out SourceUri matched, out SourceUri remaining)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, comparisonType))
        {
            if (value1.Length == value2.Length || value1[value2.Length] == '/')
            {
                matched = new SourceUri(value1.Substring(0, value2.Length));
                remaining = new SourceUri(value1[value2.Length..]);
                return true;
            }
        }
        remaining = Empty;
        matched = Empty;
        return false;
    }

    public SourceUri Add(SourceUri other)
    {
        if (HasValue &&
            other.HasValue &&
            Value[^1] == '/')
        {
            // If the path string has a trailing slash and the other string has a leading slash, we need
            // to trim one of them.
            var combined = string.Concat(Value.AsSpan(), other.Value.AsSpan(1));
            return new SourceUri(combined);
        }

        return new SourceUri(Value + other.Value);
    }

    public bool Equals(SourceUri? other)
    {
        return Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(SourceUri? other, StringComparison comparisonType)
    {
        if (ReferenceEquals(other, null))
            return false;

        if (!HasValue && !other.HasValue)
            return true;

        return string.Equals(Value, other.Value, comparisonType);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return !HasValue;
        }
        return obj is SourceUri pathString && Equals(pathString);
    }

    public override int GetHashCode()
    {
        return HasValue ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : 0;
    }

    public static bool operator ==(SourceUri? left, SourceUri? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(SourceUri? left, SourceUri? right)
        => !(left == right);

    public static string operator +(string left, SourceUri right)
    {
        // This overload exists to prevent the implicit string<->SourceUri converter from
        // trying to call the SourceUri+SourceUri operator for things that are not path strings.
        return string.Concat(left, right.ToString());
    }

    public static string operator +(SourceUri left, string? right)
    {
        // This overload exists to prevent the implicit string<->SourceUri converter from
        // trying to call the SourceUri+SourceUri operator for things that are not path strings.
        return string.Concat(left.ToString(), right);
    }

    public static SourceUri operator +(SourceUri left, SourceUri right)
    {
        return left.Add(right);
    }

    public static implicit operator SourceUri(string? s)
        => s is null ? null! : ConvertFromString(s);

    public static implicit operator string(SourceUri? path)
        => path?.ToString()!;

    internal static SourceUri ConvertFromString(string? s)
        => string.IsNullOrEmpty(s) ? new SourceUri(s) : FromUriComponent(s);

    //example from PathString
    public static SourceUri FromUriComponent(string uriComponent)
    {
        int position = uriComponent.IndexOf('%');
        if (position == -1)
        {
            return new SourceUri(uriComponent);
        }

        throw new Exception("'%' not allow symbol");

        //var value = uriComponent;

        //var indexOfInvalidChar = value.AsSpan().IndexOfAnyExcept(s_validPathChars);

        //Span<char> pathBuffer = uriComponent.Length <= StackAllocThreshold ? stackalloc char[StackAllocThreshold] : new char[uriComponent.Length];
        //uriComponent.CopyTo(pathBuffer);
        //var length = DecodeInPlace(pathBuffer.Slice(position, uriComponent.Length - position));
        //pathBuffer = pathBuffer.Slice(0, position + length);
        //return new SourceUri(pathBuffer.ToString());
    }

    public string this[int index]
    {
        get => _segments[index];
    }

    public int SegmentsCount => _segments.Length;

}

internal sealed class SourceUriConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string @string
        ? SourceUri.ConvertFromString(@string)
        : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context,
       CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);

        return destinationType == typeof(string)
            ? value?.ToString() ?? string.Empty
            : base.ConvertTo(context, culture, value, destinationType);
    }
}
