using Mars.Contracts.MetaFields;

namespace Mars.Contracts.PostCategoryTypes;

public class PostCategoryTypeEditViewModel
{
    public required PostCategoryTypeDetailResponse PostCategoryType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
