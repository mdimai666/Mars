using System.Text.Json;
using Mars.Core.Attributes;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Nodes;
using Mars.SemanticKernel.Shared.Options;
using JsonNode = System.Text.Json.Nodes.JsonNode;

namespace Mars.SemanticKernel.Shared.Nodes;

[FunctionApiDocument("./_content/Mars.SemanticKernel.Front/docs/nodes/SemanticKernelModelConfigNode/SemanticKernelModelConfigNode{.lang}.md")]
public class SemanticKernelModelConfigNode : ConfigNode
{
    public readonly static string[] AvailModelTypes = [OllamaOptions.SectionName];

    public string ModelType { get; set; } = OllamaOptions.SectionName;
    public JsonNode ModelConfig { get; set; } = JsonSerializer.SerializeToNode(new OllamaOptions())!;

    public string SystemPrompt { get; set; } = "";


    public float? Temperature { get; set; } //= 0.1f; // Температура модели. Повышение температуры заставит модель
                                            // отвечать более креативно. (По умолчанию: 0,8)
    public int? TopK { get; set; } //= 10;    // Снижает вероятность генерации бессмыслицы.Более высокое значение(например, 100)
                                   // даст более разнообразные ответы, тогда как более низкое значение (например, 10) будет более консервативным.
                                   // (По умолчанию: 40)
    public float? TopP { get; set; } //= 0.5f; // Работает вместе с top-k. Более высокое значение (например, 0,95) приведет к более разнообразному
                                     // тексту, тогда как более низкое значение (например, 0,5) сгенерирует более сфокусированный и консервативный
                                     // текст. (По умолчанию: 0,9)

    public void SetAsToolParams()
    {
        Temperature = 0.1f;
        TopK = 10;
        TopP = 20;
    }

    public void ClearParams()
    {
        Temperature = null;
        TopK = null;
        TopP = null;
    }

    public string AsDescription()
    {
        string?[] values = [$"type={ModelType}",
                            AsKeyValueIfNotEmpty("t", Temperature?.ToString()),
                            AsKeyValueIfNotEmpty("topK", TopK?.ToString()),
                            AsKeyValueIfNotEmpty("topP", TopP?.ToString()),
            ];
        return values.TrimNulls().JoinStr(", ");
    }

    static string? AsKeyValueIfNotEmpty(string key, string? value) => value == null ? null : $"{key}={value}";
}
