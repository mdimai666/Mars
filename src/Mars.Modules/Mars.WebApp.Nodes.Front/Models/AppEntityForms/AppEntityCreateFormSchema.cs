using Mars.Shared.Models;

namespace Mars.WebApp.Nodes.Front.Models.AppEntityForms;

public record AppEntityCreateFormSchema
{
    public required string Title { get; init; }
    public required SourceUri EntityUri { get; init; }
    public required IReadOnlyCollection<EntityPropertyFormField> Properties { get; init; }
}

public record EntityPropertyFormField
{
    public required string Title { get; init; }
    public required string PropertyName { get; init; }
    public required string Placeholder { get; init; }
    public required bool IsRequired { get; init; }

}
