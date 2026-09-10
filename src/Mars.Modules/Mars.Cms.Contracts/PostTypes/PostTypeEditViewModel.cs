using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Contracts.PostTypes;

public class PostTypeEditViewModel
{
    public required PostTypeDetailResponse PostType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
