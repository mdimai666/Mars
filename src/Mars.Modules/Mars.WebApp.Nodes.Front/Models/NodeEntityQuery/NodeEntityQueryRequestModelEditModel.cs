using Mars.Core.Extensions;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.WebApp.Nodes.Models.NodeEntityQuery;

namespace Mars.WebApp.Nodes.Front.Models.NodeEntityQuery;

public class NodeEntityQueryRequestModelEditModel
{
    public string EntityName { get; set; } = "";
    public NEQRM_MethodCallEditModel[] CallChains { get; set; } = [];

    public NodeEntityQueryBuilderDictionary ProvidersDict { get; init; }

    public NodeEntityQueryRequestModelEditModel(NodeEntityQueryBuilderDictionary providersDict, NodeEntityQueryRequestModel value)
    {
        EntityName = value.EntityName.AsNullIfEmpty() ?? providersDict.Providers.First().Key;
        ProvidersDict = providersDict;

        //TODO: учитывать что сигнатура метода может исчезнуть
        CallChains = value.CallChains.Select(s => new NEQRM_MethodCallEditModel()
        {
            Method = providersDict.Providers.GetValueOrDefault(EntityName)?.Methods.FirstOrDefault(m => m.Name == s.MethodName),
            Disabled = s.Disabled,
            Parameters = s.Parameters,
        }).ToArray();
    }

    public NodeEntityQueryRequestModel ToRequest()
        => new()
        {
            EntityName = EntityName,
            CallChains = CallChains.Select(s => s.ToRequest()).ToArray(),
        };
}

public class NEQRM_MethodCallEditModel
{
    public required List<string> Parameters { get; set; }
    public required LinqMethodSignarute? Method { get; init; }
    public required bool Disabled { get; set; }

    public NEQRM_MethodCall ToRequest()
        => new()
        {
            MethodName = Method.Name,
            Parameters = Parameters,
            Disabled = Disabled
        };
}
