using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Определение блока: источник правды, из которого генерируется Blockly JSON definition.
/// Объявляется fluent-API (<see cref="PxMaster.Define"/>) либо наследованием для блоков
/// с динамической структурой; исполнение блоков будет жить отдельно (по образцу INodeImplement).
/// </summary>
public partial class PxBlockDefinition
{
    public string TypeId { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";
    public string Tooltip { get; set; } = "";

    /// <summary>Строки message0/args0, message1/args1, … в терминах Blockly.</summary>
    public List<PxMessageRow> Messages { get; set; } = [];

    /// <summary>Тип выходного коннектора. null — блок-оператор (statement).</summary>
    public string? OutputType { get; set; }

    public bool HasPrevious { get; set; } = true;
    public bool HasNext { get; set; } = true;

    /// <summary>Имена расширений Blockly (Blockly.Extensions.register).</summary>
    public List<string> Extensions { get; set; } = [];

    /// <summary>Имя мутатора Blockly (Blockly.Extensions.registerMutator) — для блоков с динамической структурой.</summary>
    public string? Mutator { get; set; }

    /// <summary>
    /// «Шапка» хат-блока: "cap" — скруглённый верх событийных блоков. В Blockly JSON
    /// уходит расширением px_hat_{Hat} (а не style.hat — тот jsonInit Blockly читает
    /// один раз и обнуляет в общем определении, шапка досталась бы лишь первому
    /// созданному экземпляру блока). Расширение регистрирует сторона JS (Workspace).
    /// </summary>
    public string? Hat { get; set; }

    public virtual string ToJson()
    {
        var node = new JsonObject { ["type"] = TypeId };

        for (var i = 0; i < Messages.Count; i++)
        {
            var (message, args) = ResolveMessage(Messages[i]);
            node[$"message{i}"] = message;
            if (args.Count > 0)
                node[$"args{i}"] = new JsonArray(args.Select(a => a.ToJsonNode()).ToArray());
        }

        if (!string.IsNullOrEmpty(Tooltip))
            node["tooltip"] = Tooltip;

        node["colour"] = Colour;

        if (OutputType != null)
        {
            node["output"] = OutputType;
        }
        else
        {
            if (HasPrevious)
                node["previousStatement"] = null;
            if (HasNext)
                node["nextStatement"] = null;
        }

        var extensions = new List<string>(Extensions);
        if (Hat != null)
            extensions.Add($"px_hat_{Hat}");
        if (extensions.Count > 0)
            node["extensions"] = new JsonArray(extensions.Select(e => (JsonNode?)e).ToArray());

        if (Mutator != null)
            node["mutator"] = Mutator;

        return node.ToJsonString();
    }

    public static string ToArrayJson(IEnumerable<PxBlockDefinition> definitions) =>
        new JsonArray(definitions.Select(d => JsonNode.Parse(d.ToJson())).ToArray()).ToJsonString();

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex NamedHoleRegex();

    /// <summary>
    /// Именованные плейсхолдеры {имя}: порядок аргументов выводится из строки сообщения,
    /// они заменяются на %1..%N для Blockly. Без плейсхолдеров {…} строка считается
    /// позициянной (%1..%N) и аргументы идут в порядке объявления.
    /// </summary>
    private (string Message, List<PxArg> Args) ResolveMessage(PxMessageRow row)
    {
        var holes = NamedHoleRegex().Matches(row.Message);
        if (holes.Count == 0)
            return (row.Message, row.Args);

        var byName = new Dictionary<string, PxArg>();
        foreach (var arg in row.Args)
        {
            if (!byName.TryAdd(arg.Name, arg))
                throw new InvalidOperationException($"Блок '{TypeId}': аргумент '{arg.Name}' объявлен дважды.");
        }

        var ordered = new List<PxArg>(holes.Count);
        foreach (Match hole in holes)
        {
            var name = hole.Groups[1].Value;
            if (ordered.Any(a => a.Name == name))
                throw new InvalidOperationException($"Блок '{TypeId}': плейсхолдер '{{{name}}}' встречается в сообщении больше одного раза.");
            if (!byName.TryGetValue(name, out var arg))
                throw new InvalidOperationException($"Блок '{TypeId}': плейсхолдер '{{{name}}}' в сообщении не объявлен как аргумент.");
            ordered.Add(arg);
        }

        var unused = row.Args.Where(a => !ordered.Contains(a)).Select(a => a.Name).ToList();
        if (unused.Count > 0)
            throw new InvalidOperationException(
                $"Блок '{TypeId}': аргументы {string.Join(", ", unused)} не использованы в сообщении.");

        var message = row.Message;
        for (var i = 0; i < ordered.Count; i++)
            message = message.Replace($"{{{ordered[i].Name}}}", $"%{i + 1}");

        return (message, ordered);
    }
}
