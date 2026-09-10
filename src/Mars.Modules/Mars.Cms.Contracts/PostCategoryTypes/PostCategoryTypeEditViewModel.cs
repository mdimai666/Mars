using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Contracts.PostCategoryTypes;

public class PostCategoryTypeEditViewModel
{
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
