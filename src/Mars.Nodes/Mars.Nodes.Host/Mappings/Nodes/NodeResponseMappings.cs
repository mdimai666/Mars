using Mars.Nodes.Core;
using Mars.Nodes.Core.Contracts.Nodes;
using Mars.Nodes.Core.Models;
using Mars.Nodes.Core.Nodes.Functions;

namespace Mars.Nodes.Host.Mappings.Nodes;

public static class NodeResponseMappings
{
    public static NodesDataResponse ToResponse(this NodesData entity)
        => new()
        {
            Nodes = entity.Nodes.ToArray(),
            NodesState = entity.Nodes.Where(s => s.status != null).ToDictionary(node => node.Id, node => node.ToResponse()),
            InlineFunctionNodeSchemas = entity.InlineFunctionNodeSchemas.ToResponse()
        };

    public static NodeStateInfoResponse ToResponse(this Node entity)
        => new()
        {
            Status = entity.status
        };

    public static InlineFunctionNodeSchemaResponse ToResponse(this InlineFunctionNodeSchema entity)
        => new()
        {
            TypeId = entity.TypeId,
            Name = entity.Name,
            Color = entity.Color,
            Icon = entity.Icon,
            GroupName = entity.GroupName,
            Inputs = entity.Inputs,
            Outputs = entity.Outputs,
            Parameters = entity.Parameters,
        };

    public static IReadOnlyCollection<InlineFunctionNodeSchemaResponse> ToResponse(this IEnumerable<InlineFunctionNodeSchema> list)
        => list.Select(ToResponse).ToArray();

}
