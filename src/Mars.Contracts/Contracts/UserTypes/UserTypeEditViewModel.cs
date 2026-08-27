using Mars.Contracts.MetaFields;

namespace Mars.Contracts.UserTypes;

public class UserTypeEditViewModel
{
    public required UserTypeDetailResponse UserType { get; init; }
    public required IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; }

}
