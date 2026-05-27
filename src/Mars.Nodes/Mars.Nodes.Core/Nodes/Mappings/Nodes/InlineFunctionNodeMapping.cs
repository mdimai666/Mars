using Mars.Nodes.Core.Contracts.Nodes;

namespace Mars.Nodes.Core.Nodes.Mappings.Nodes;

public static class InlineFunctionNodeMapping
{
    public static InlineFunctionNodeSchema ToModel(this InlineFunctionNodeSchemaResponse entity)
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
}
