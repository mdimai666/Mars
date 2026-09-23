using Mars.Nodes.Core.Nodes.Functions;

namespace Mars.Nodes.Contracts.Nodes;

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
