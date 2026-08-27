using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.UserTypes;

public class UserTypeEditViewModel
{
    public required UserTypeDetailResponse UserType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
