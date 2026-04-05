using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Mars.Nodes.Core.Implements.Models;

/// <summary>
/// Класс для удобного доступа к полям JsonNode
/// </summary>
public class JsonPathAccessor
{
    private readonly JsonNode _root;

    public JsonPathAccessor(JsonNode root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    // 🔹 Кеш парсинга путей
    private static readonly ConcurrentDictionary<string, List<PathToken>> _cache = new();

    // 🔹 Публичное API

    public JsonNode? GetNode(string path)
    {
        var tokens = GetTokens(path);
        JsonNode? current = _root;

        foreach (var token in tokens)
        {
            if (current == null) return null;

            if (token.IsIndex)
            {
                if (current is JsonArray arr && token.Index < arr.Count)
                    current = arr[token.Index];
                else
                    return null;
            }
            else
            {
                if (current is JsonObject obj && obj.TryGetPropertyValue(token.Property!, out var value))
                    current = value;
                else
                    return null;
            }
        }

        return current;
    }

    public object? Get(string path)
        => Unwrap(GetNode(path));

    public T GetValue<T>(string path)
        => GetNode(path).GetValue<T>();

    public void Set(string path, object? value)
    {
        var tokens = GetTokens(path);
        JsonNode current = _root;

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];

            if (token.IsIndex)
            {
                var arr = current as JsonArray ?? throw new Exception($"Expected array at [{token.Index}]");

                while (arr.Count <= token.Index)
                    arr.Add(null);

                current = arr[token.Index] ??= new JsonObject();
            }
            else
            {
                var obj = current as JsonObject ?? throw new Exception($"Expected object at '{token.Property}'");

                if (!obj.TryGetPropertyValue(token.Property!, out var next) || next == null)
                {
                    next = CreateNode(tokens[i + 1]);
                    obj[token.Property!] = next;
                }

                current = next;
            }
        }

        var last = tokens[^1];

        if (last.IsIndex)
        {
            var arr = current as JsonArray ?? throw new Exception($"Expected array at [{last.Index}]");

            while (arr.Count <= last.Index)
                arr.Add(null);

            arr[last.Index] = Wrap(value);
        }
        else
        {
            var obj = current as JsonObject ?? throw new Exception($"Expected object at '{last.Property}'");
            obj[last.Property!] = Wrap(value);
        }
    }

    public bool Exists(string path) => Get(path) != null;

    public void Remove(string path)
    {
        var tokens = GetTokens(path);
        JsonNode current = _root;

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var token = tokens[i];

            current = (token.IsIndex
                ? (current as JsonArray)?[token.Index]
                : (current as JsonObject)?[token.Property!])!;

            if (current == null) return;
        }

        var last = tokens[^1];

        if (last.IsIndex && current is JsonArray arr && last.Index < arr.Count)
        {
            arr.RemoveAt(last.Index);
        }
        else if (!last.IsIndex && current is JsonObject obj)
        {
            obj.Remove(last.Property!);
        }
    }

    // 🔹 Вспомогательное

    private static List<PathToken> GetTokens(string path)
    {
        return _cache.GetOrAdd(path, ParsePath);
    }

    private static JsonNode? Wrap(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode node => node,
            _ => JsonValue.Create(value)
        };
    }

    private static object? Unwrap(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonValue v => v.GetValue<object>(),
            _ => node
        };
    }

    private static JsonNode CreateNode(PathToken next)
    {
        return next.IsIndex ? new JsonArray() : new JsonObject();
    }

    // 🔹 Парсер пути

    private static List<PathToken> ParsePath(string path)
    {
        var tokens = new List<PathToken>();
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            var current = part;

            while (true)
            {
                var bracketIndex = current.IndexOf('[');

                if (bracketIndex == -1)
                {
                    if (!string.IsNullOrEmpty(current))
                        tokens.Add(new PathToken(current));
                    break;
                }

                if (bracketIndex > 0)
                {
                    tokens.Add(new PathToken(current[..bracketIndex]));
                }

                var endBracket = current.IndexOf(']');
                var index = int.Parse(current[(bracketIndex + 1)..endBracket]);

                tokens.Add(new PathToken(null, index));

                current = current[(endBracket + 1)..];

                if (string.IsNullOrEmpty(current))
                    break;
            }
        }

        return tokens;
    }

    private record PathToken(string? Property, int Index = -1)
    {
        public bool IsIndex => Index >= 0;
    }
}
