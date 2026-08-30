namespace Mars.WebApp.Nodes.Models.AppEntityForms;

public record AppEntityCreateFormsBuilderDictionary
{
    public required IReadOnlyCollection<AppEntityCreateFormSchema> Forms { get; init; }
}
