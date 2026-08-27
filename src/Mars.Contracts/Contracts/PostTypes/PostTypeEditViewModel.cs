using Mars.Contracts.MetaFields;

namespace Mars.Contracts.PostTypes;

public class PostTypeEditViewModel
{
    public required PostTypeDetailResponse PostType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
