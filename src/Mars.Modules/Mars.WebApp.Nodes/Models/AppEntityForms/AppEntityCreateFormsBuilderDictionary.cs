namespace Mars.WebApp.Nodes.Front.Models.AppEntityForms;

public record AppEntityCreateFormsBuilderDictionary
{
    public required IReadOnlyCollection<AppEntityCreateFormSchema> Forms { get; init; }
}
