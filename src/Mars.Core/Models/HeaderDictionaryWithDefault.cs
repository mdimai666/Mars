using System.Xml.Linq;

public class HeaderDictionaryWithDefault<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
    TValue _default = default!;
    public TValue DefaultValue
    {
        get { return _default; }
        set { _default = value; }
    }
    public HeaderDictionaryWithDefault() : base() { }

    public HeaderDictionaryWithDefault(IEqualityComparer<TKey> comparer) : base(comparer) { }
    public HeaderDictionaryWithDefault(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

    public HeaderDictionaryWithDefault(TValue defaultValue) : base()
    {
        _default = defaultValue;
    }
    public new TValue this[TKey key]
    {
        get
        {
            //TValue t;
            return base.TryGetValue(key, out var t) ? t : _default;
        }
        set { base[key] = value; }
    }
}

/// <summary>
/// <para>
/// В спецификации протокола HTTP ключи заголовков могут повторяться, потому что это стандартный способ передачи нескольких значений для одного и того же параметра.
/// </para>
/// <para>
/// 1. Стандарт протокола HTTPПо спецификации RFC (в частности, RFC 9110), если у заголовка есть несколько значений, сервер или клиент имеют право:
/// <br/>
/// Либо прислать один заголовок, перечислив значения через запятую:
///<list>
///<item>
/// Accept: text/html, application/xhtml+xml
///</item>
///</list>
/// Либо продублировать строки заголовка:
///<list>
///<item>
/// Accept: text/html
/// </item>
/// <item>
/// Accept: application/xhtml+xml
/// </item>
/// </list>
/// </para>
/// </summary>
public static class HeaderDictionaryWithDefaultExtension
{
    // Версия специально для string значений (для склеивания через запятую)
    public static HeaderDictionaryWithDefault<TKey, string> ToHeaderDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, string> elementSelector,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(keySelector, nameof(keySelector));
        ArgumentNullException.ThrowIfNull(elementSelector, nameof(elementSelector));

        // Используем OrdinalIgnoreCase по умолчанию для строк, если компаратор не задан
        comparer ??= (typeof(TKey) == typeof(string)
            ? (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase
            : EqualityComparer<TKey>.Default);

        // Пытаемся получить capacity из коллекции, чтобы избежать лишних аллокаций
        int capacity = source is ICollection<TSource> collection ? collection.Count : 0;
        var dictionary = new HeaderDictionaryWithDefault<TKey, string>(capacity, comparer);

        foreach (TSource item in source)
        {
            TKey key = keySelector(item);
            string newValue = elementSelector(item);

            // Если ключ уже есть — склеиваем через запятую, иначе — просто добавляем
            if (dictionary.TryGetValue(key, out string? existingValue))
            {
                dictionary[key] = $"{existingValue},{newValue}";
            }
            else
            {
                dictionary[key] = newValue;
            }
        }

        return dictionary;
    }

    public static HeaderDictionaryWithDefault<TKey, TElement> ToDefaultObjectDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(keySelector, nameof(keySelector));
        ArgumentNullException.ThrowIfNull(elementSelector, nameof(elementSelector));

        // Используем OrdinalIgnoreCase по умолчанию для строк, если компаратор не задан
        comparer ??= (typeof(TKey) == typeof(string)
            ? (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase
            : EqualityComparer<TKey>.Default);

        // Пытаемся получить capacity из коллекции, чтобы избежать лишних аллокаций
        int capacity = source is ICollection<TSource> collection ? collection.Count : 0;
        var dictionary = new HeaderDictionaryWithDefault<TKey, TElement>(capacity, comparer);

        foreach (TSource item in source)
        {
            TKey key = keySelector(item);
            TElement newValue = elementSelector(item);

            dictionary[key] = newValue;
        }

        return dictionary;
    }
}
