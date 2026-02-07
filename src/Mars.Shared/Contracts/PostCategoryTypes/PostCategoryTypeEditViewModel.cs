using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostCategoryTypes;

public class PostCategoryTypeEditViewModel
{
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
