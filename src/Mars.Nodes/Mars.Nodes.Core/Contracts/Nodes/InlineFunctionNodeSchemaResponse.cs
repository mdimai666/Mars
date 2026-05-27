using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Contracts.Nodes;

public record InlineFunctionNodeSchemaResponse
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public required string? Color { get; init; }
    public required string? Icon { get; init; }

    public required string GroupName { get; init; }

    public required NodeInput[] Inputs { get; init; }
    public required NodeOutput[] Outputs { get; init; }

    public required IFNS_Parameter[] Parameters { get; init; }
}
