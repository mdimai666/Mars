using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Mappings.Users;

public static class UserEditProfileMapping
{
    public static UserEditProfileResponse ToResponse(this UserEditProfileDto entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            About = entity.About,
            AvatarUrl = entity.AvatarUrl,
            BirthDate = entity.BirthDate,
            Gender = entity.Gender,
            Phone = entity.Phone,
            Type = entity.Type,
            MetaValues = entity.MetaValues.ToDetailResponse(),
        };

    public static UserProfileInfoResponse ToResponse(this UserProfileInfoDto entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            About = entity.About,
            AvatarUrl = entity.AvatarUrl,
            BirthDate = entity.BirthDate,
            Gender = entity.Gender,
            Phone = entity.Phone,

            CommentCount = entity.CommentCount,
            Roles = entity.Roles.ToArray(),
            Type = entity.Type,
            MetaValues = entity.MetaValues.ToDetailResponse(),
        };
}
