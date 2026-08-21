using Mars.Core.Features;
using Mars.Shared.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class FormMetaField
{
    [CascadingParameter]
    public List<MetaFieldEditModel> Model { get; set; } = default!;

    [CascadingParameter]
    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = default!;

    [Inject] IMarsWebApiClient client { get; set; } = default!;

    void OnChangeFieldTitle(string value, MetaFieldEditModel model)
    {
        model.Title = value;
        if (string.IsNullOrWhiteSpace(model.Key))
        {
            model.Key = TextTool.TranslateToPostSlug(model.Title);
        }
    }

    public void UpdateState()
    {
        StateHasChanged();
    }

    void OnDelete(MetaFieldEditModel field)
    {
        Model.Remove(field);
    }

    public static MetaFieldEditModel NewField(int order)
    {
        return new MetaFieldEditModel
        {
            Id = Guid.NewGuid(),
            Order = order,
        };
    }
}
