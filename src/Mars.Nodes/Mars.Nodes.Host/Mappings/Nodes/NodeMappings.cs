using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.StringFunctions;
using Mars.Nodes.Host.Shared.Dto;

namespace Mars.Nodes.Host.Mappings.Nodes;

public static class NodeMappings
{
    public static NodesDataDto ToNodeDataDto(this IEnumerable<Node> nodes, IReadOnlyCollection<InlineFunctionNodeSchema> inlineFunctionNodeSchemas)
        => new()
        {
            Nodes = nodes.ToArray(),
            NodesState = nodes.Where(s => s.status != null).ToDictionary(node => node.Id, node => node.ToNodeStateInfoDto()),
            InlineFunctionNodeSchemas = inlineFunctionNodeSchemas
        };

    public static NodeStateInfoDto ToNodeStateInfoDto(this Node node)
        => new()
        {
            Status = node.status
        };

    public static InlineFunctionNodeSchema ToSchema(this InlineFunctionNodeDefinition entity)
        => new()
        {
            TypeId = entity.TypeId,
            Name = entity.Name,
            Color = entity.Color,
            Icon = entity.Icon,
            GroupName = entity.GroupName,
            Inputs = entity.Inputs,
            Outputs = entity.Outputs,
            Parameters = entity.Delegate.ToSchema(),
        };

    public static IFNS_Parameter[] ToSchema(this Delegate entity)
    {
        return entity.Method.GetParameters().Select(p => new IFNS_Parameter
        {
            Name = p.Name!,
            Type = StringValueParser.TypeToTypeCode(p.ParameterType),
            TypeName = p.ParameterType.Name,
            DefaultValue = p.DefaultValue?.ToString(),
            IsRequired = !p.IsOptional,
            Description = p.GetCustomAttribute<DisplayAttribute>()?.Description,
        }).ToArray();
    }

    public static InlineFunctionNodeSchema[] ToSchema(this IEnumerable<InlineFunctionNodeDefinition> list)
        => list.Select(ToSchema).ToArray();

}
