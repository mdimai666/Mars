using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Types;

public class PxTypeRegistry
{
    public List<PxType> Types { get; set; } = [];

    public string ToJson()
    {
        var root = new JsonObject
        {
            ["types"] = new JsonArray(Types.Select(t => (JsonNode?)new JsonObject
            {
                ["name"] = t.Name,
                ["shape"] = ShapeName(t.Shape),
                ["compatibleWith"] = new JsonArray(t.CompatibleWith.Select(c => (JsonNode?)c).ToArray()),
            }).ToArray()),
        };
        return root.ToJsonString();
    }

    private static string ShapeName(PxShape shape) => shape switch
    {
        PxShape.Hexagonal => "hexagonal",
        PxShape.Square => "square",
        _ => "rounded",
    };

    /// <summary>
    /// Базовый набор в духе PXT: Boolean → шестиугольник, Number/String → скругление,
    /// Array → квадрат; Any и Object — расширения под нашу систему.
    /// </summary>
    public static PxTypeRegistry CreateDefault() => new()
    {
        Types =
        [
            new PxType { Name = "Boolean", Shape = PxShape.Hexagonal },
            new PxType { Name = "Number" },
            new PxType { Name = "String" },
            new PxType { Name = "Array", Shape = PxShape.Square },
            new PxType { Name = "Any", CompatibleWith = ["*"] },
            new PxType { Name = "Object", Shape = PxShape.Square }
        ]
    };
}
