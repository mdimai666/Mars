using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.Repositories.Mappings;

internal static class UserMapping
{
    public static UserSummary ToSummary(this UserEntity entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
        };

    public static UserDetail ToDetail(this UserEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            UserName = entity.UserName ?? "xxxx",
            BirthDate = entity.BirthDate,
            Gender = entity.Gender.ToMap(),
            Roles = entity.Roles!.Select(s => s.Name).ToArray()!
        };

    public static IReadOnlyCollection<UserSummary> ToSummaryList(this IEnumerable<UserEntity> entities)
        => entities.Select(ToSummary).ToList();

    public static IReadOnlyCollection<UserDetail> ToDetailList(this IEnumerable<UserEntity> entities)
        => entities.Select(ToDetail).ToList();

    public static UserEditProfileDto ToProfile(this UserEntity entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            Username = entity.UserName ?? "xxxx",
            About = "",
            AvatarUrl = "",
            BirthDate = entity.BirthDate?.Date,
            Gender = entity.Gender.ToMap(),
            Phone = entity.PhoneNumber,
        };

    public static Mars.Shared.Contracts.Users.UserGender ToMap(this Mars.Host.Data.OwnedTypes.Users.UserGender gender)
        => (Mars.Shared.Contracts.Users.UserGender)gender;
    public static Mars.Host.Data.OwnedTypes.Users.UserGender ToMap(this Mars.Shared.Contracts.Users.UserGender gender)
        => (Mars.Host.Data.OwnedTypes.Users.UserGender)gender;
}
