using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.UserTypes;

namespace Mars.Host.Repositories.Mappings;

internal static class UserTypeMapping
{
    public static UserTypeSummary ToSummary(this UserTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static UserTypeDetail ToDetail(this UserTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,
            MetaFields = entity.MetaFields!.ToDto()
        };

    public static IReadOnlyCollection<UserTypeSummary> ToSummaryList(this IEnumerable<UserTypeEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<UserTypeDetail> ToDetailList(this IEnumerable<UserTypeEntity> entities)
        => entities.Select(ToDetail).ToArray();

    public static UserTypeEntity ToEntity(this CreateUserTypeQuery query)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            Title = query.Title,
            TypeName = query.TypeName,
            Tags = query.Tags.ToList(),

            MetaFields = query.MetaFields.ToEntity()
        };

}
