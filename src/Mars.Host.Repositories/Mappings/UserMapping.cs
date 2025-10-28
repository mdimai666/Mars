using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.Users;
using Mars.Host.Shared.Dto.Profile;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.Dto.Users;
using Microsoft.AspNetCore.Identity;

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
            Roles = entity.Roles!.Select(s => s.Name).ToArray()!,
            Type = entity.UserType.TypeName,
            MetaValues = entity.MetaValues!.ToDetailDto(),
        };

    public static UserEditDetail ToEditDetail(this UserEntity entity)
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
            Roles = entity.Roles!.Select(s => s.Name).ToArray()!,
            Type = entity.UserType.TypeName,
            MetaValues = entity.MetaValues!.ToDetailDto(),
            UserTypeDetail = entity.UserType.ToDetail(),
        };

    public static IReadOnlyCollection<UserSummary> ToSummaryList(this IEnumerable<UserEntity> entities)
        => entities.Select(ToSummary).ToList();

    public static IReadOnlyCollection<UserDetail> ToDetailList(this IEnumerable<UserEntity> entities)
        => entities.Select(ToDetail).ToList();

    public static UserProfileDto ToProfile(this UserEntity entity)
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
            Roles = entity.Roles!.Select(s => s.Name).ToArray()!,
            Type = entity.UserType.TypeName,
            MetaValues = entity.MetaValues!.ToDetailDto(),

            AvatarUrl = "",
            About = "",
        };

    public static UserEditProfileDto ToEditProfile(this UserEntity entity)
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
            Type = entity.UserType.TypeName,
            MetaValues = entity.MetaValues!.ToDetailDto(),
        };

    public static Mars.Shared.Contracts.Users.UserGender ToMap(this Mars.Host.Data.OwnedTypes.Users.UserGender gender)
        => (Mars.Shared.Contracts.Users.UserGender)gender;
    public static Mars.Host.Data.OwnedTypes.Users.UserGender ToMap(this Mars.Shared.Contracts.Users.UserGender gender)
        => (Mars.Host.Data.OwnedTypes.Users.UserGender)gender;

    public static UserEntity ToEntity(this CreateUserQuery query, Guid userTypeId, ILookupNormalizer lookupNormalizer)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            UserName = query.UserName ?? query.Email,
            NormalizedUserName = lookupNormalizer.NormalizeName(query.UserName ?? query.Email),

            Email = query.Email,
            NormalizedEmail = lookupNormalizer.NormalizeEmail(query.Email),

            FirstName = query.FirstName,
            LastName = query.LastName ?? "",

            EmailConfirmed = true,
            LockoutEnabled = true,

            PhoneNumber = query.PhoneNumber,
            BirthDate = query.BirthDate,
            Gender = ParseGender(query.Gender),
            UserTypeId = userTypeId,
            MetaValues = query.MetaValues.ToEntity(),

        };

    public static UserEntity UpdateEntity(this UserEntity entity, UpdateUserQuery query)
    {
        entity.FirstName = query.FirstName;
        entity.LastName = query.LastName ?? "";
        entity.MiddleName = query.MiddleName;
        entity.Email = query.Email;

        entity.PhoneNumber = query.PhoneNumber;
        entity.BirthDate = query.BirthDate;
        entity.Gender = UserMapping.ParseGender(query.Gender);

        entity.ModifiedAt = DateTimeOffset.Now;
        return entity;
    }

    public static UserGender ParseGender(Mars.Shared.Contracts.Users.UserGender gender)
        => gender switch
        {
            Mars.Shared.Contracts.Users.UserGender.None => UserGender.None,
            Mars.Shared.Contracts.Users.UserGender.Male => UserGender.Male,
            Mars.Shared.Contracts.Users.UserGender.Female => UserGender.Female,
            _ => throw new NotImplementedException(),
        };

    public static AuthorizedUserInformationDto ToDto(this UserEntity entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            UserName = entity.UserName!,
            BirthDate = entity.BirthDate,
            Gender = entity.Gender.ToMap(),
            Roles = entity.Roles!.Select(s => s.Name).ToArray()!,
            SecurityStamp = entity.SecurityStamp!
        };

    public static UserEntity ToEntity(this UpsertUserRemoteDataQuery query, Guid userTypeId, ILookupNormalizer lookupNormalizer)
        => new()
        {
            //Id = query.Id ?? Guid.Empty,
            UserName = query.PreferredUserName ?? query.Email,
            NormalizedUserName = lookupNormalizer.NormalizeName(query.PreferredUserName ?? query.Email),

            Email = query.Email,
            NormalizedEmail = lookupNormalizer.NormalizeEmail(query.Email),

            FirstName = query.FirstName,
            LastName = query.LastName ?? "",

            EmailConfirmed = true,
            LockoutEnabled = true,

            PhoneNumber = query.PhoneNumber,
            BirthDate = query.BirthDate,
            Gender = ParseGender(query.Gender),
            UserTypeId = userTypeId,
        };

    public static UserEntity UpdateEntity(this UserEntity entity, UpsertUserRemoteDataQuery query)
    {
        entity.FirstName = query.FirstName;
        entity.LastName = query.LastName ?? "";
        entity.MiddleName = query.MiddleName;
        entity.Email = query.Email;

        entity.PhoneNumber = query.PhoneNumber;
        entity.BirthDate = query.BirthDate;
        entity.Gender = UserMapping.ParseGender(query.Gender);

        entity.ModifiedAt = DateTimeOffset.Now;
        return entity;
    }

    public static bool UserInfoHasChanges(this UserEntity entity, UpsertUserRemoteDataQuery query)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(query);

        return
            entity.FirstName != query.FirstName ||
            entity.LastName != (query.LastName ?? "") ||
            entity.MiddleName != query.MiddleName ||
            entity.Email != query.Email ||
            entity.PhoneNumber != query.PhoneNumber ||
            entity.BirthDate != query.BirthDate ||
            entity.Gender != UserMapping.ParseGender(query.Gender);
    }
}
