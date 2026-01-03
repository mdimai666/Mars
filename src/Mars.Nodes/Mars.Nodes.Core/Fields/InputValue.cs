using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars.Nodes.Core.Converters;

namespace Mars.Nodes.Core.Fields;

[TypeConverter(typeof(InputValueTypeConverter<string>))]
public class InputValue<T>
{
    private static readonly Regex SingleNameRegex =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public string ValueOrExpression { get; init; } = "";

    /// <summary>
    /// true, если значение является выражением
    /// </summary>
    public bool IsEvalExpression { get; init; }

    /// <summary>
    /// true, если выражение — одиночное имя переменной (например @value)
    /// </summary>
    public bool IsSingleName { get; init; }

    /// <summary>
    /// true, если значение — JSON literal
    /// </summary>
    public bool IsJsonLiteral { get; init; }

    public override string ToString()
    {
        if (IsEvalExpression)
            return "@" + ValueOrExpression;

        // escape '@' при сериализации
        if (ValueOrExpression.StartsWith("@", StringComparison.Ordinal))
            return "@@" + ValueOrExpression.Substring(1);

        return ValueOrExpression;
    }

    public static InputValue<T> Parse(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Trim();

        // 1. @@ escape
        if (value.StartsWith("@@", StringComparison.Ordinal))
        {
            return new InputValue<T>
            {
                IsEvalExpression = false,
                IsSingleName = false,
                IsJsonLiteral = false,
                ValueOrExpression = value.Substring(1)
            };
        }

        // 2. JSON literal
        if (IsJson(value))
        {
            return new InputValue<T>
            {
                IsEvalExpression = false,
                IsSingleName = false,
                IsJsonLiteral = true,
                ValueOrExpression = value
            };
        }

        // 3. Expression
        if (value.StartsWith("@", StringComparison.Ordinal))
        {
            var raw = value.Substring(1);

            return new InputValue<T>
            {
                IsEvalExpression = true,
                ValueOrExpression = raw,
                IsSingleName = IsSingleNameCandidate(raw),
                IsJsonLiteral = false
            };
        }

        // 4. Plain literal
        return new InputValue<T>
        {
            IsEvalExpression = false,
            IsSingleName = false,
            IsJsonLiteral = false,
            ValueOrExpression = value
        };
    }

    private static bool IsSingleNameCandidate(string value)
        => SingleNameRegex.IsMatch(value);

    private static bool IsJson(string value)
    {
        if (value.Length < 2)
            return false;

        var first = value[0];
        var last = value[^1];

        // quick check
        if (!((first == '{' && last == '}') ||
              (first == '[' && last == ']') ||
              (first == '"' && last == '"')))
            return false;

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // implicit conversion to string
    public static implicit operator string(InputValue<T> value)
        => value.ToString();

    // implicit conversion from string
    public static implicit operator InputValue<T>(string value)
        => Parse(value);

    public bool Equals(InputValue<T>? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(
            ToString(),
            other.ToString(),
            StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
        => obj is InputValue<T> other && Equals(other);

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(ToString());

    public static bool operator ==(InputValue<T>? left, InputValue<T>? right)
        => Equals(left, right);

    public static bool operator !=(InputValue<T>? left, InputValue<T>? right)
        => !Equals(left, right);
}

//public struct OutputProperty
//{
//    public string PropertyName { get; set; } = "Payload";
//}
