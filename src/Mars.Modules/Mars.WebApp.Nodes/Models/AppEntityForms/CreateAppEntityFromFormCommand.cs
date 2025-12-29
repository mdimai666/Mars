using Mars.Shared.Models;

namespace Mars.WebApp.Nodes.Models.AppEntityForms;

public record CreateAppEntityFromFormCommand
{
    public required SourceUri EntityUri { get; init; }

    public required IReadOnlyCollection<EntityPropertyBinding> PropertyBindings { get; init; }
}

public record EntityPropertyBinding
{
    public required string PropertyName { get; init; }
    public required string ValueOrExpression { get; init; }
    public required bool IsEvalExpression { get; init; }
}
