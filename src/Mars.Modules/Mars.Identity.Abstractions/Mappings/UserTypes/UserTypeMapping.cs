using Mars.Data.Extensions;
using Mars.Identity.Abstractions.Dto.UserTypes;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.UserTypes;

namespace Mars.Identity.Abstractions.Mappings.UserTypes;

public static class UserTypeMapping
{
    public static UserTypeSummaryResponse ToResponse(this UserTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            //MetaFields = entity.MetaFields.ToResponse(),
        };

    public static UserTypeDetailResponse ToResponse(this UserTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            MetaFields = entity.MetaFields.ToDetailResponse(),
        };

    public static UserTypeSummary ToSummary(this UserTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static UserTypeSummaryResponse ToSummaryResponse(this UserTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static UserTypeListItemResponse ToItemResponse(this UserTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            TypeName = entity.TypeName,
            CreatedAt = entity.CreatedAt,
            Tags = entity.Tags,
        };

    public static ListDataResult<UserTypeListItemResponse> ToResponse(this ListDataResult<UserTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static PagingResult<UserTypeListItemResponse> ToResponse(this PagingResult<UserTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static IReadOnlyCollection<UserTypeSummaryResponse> ToResponse(this IReadOnlyCollection<UserTypeSummary> list)
        => list.Select(ToResponse).ToList();

}
