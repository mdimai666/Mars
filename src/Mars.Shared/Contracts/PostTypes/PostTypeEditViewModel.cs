using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.PostTypes;

public class PostTypeEditViewModel
{
    public required PostTypeDetailResponse PostType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
