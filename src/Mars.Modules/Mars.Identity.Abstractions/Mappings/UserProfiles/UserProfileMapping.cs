using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Contracts.Users.UserProfiles;

namespace Mars.Identity.Abstractions.Mappings.UserProfiles;

public static class UserProfileMapping
{
    public static UserProfileResponse ToResponse(this UserProfileDto entity)
        => new()
        {
            Id = entity.Id,
            UserName = entity.UserName,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleName = entity.MiddleName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Roles = entity.Roles,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,

            BirthDate = entity.BirthDate,
            Gender = entity.Gender,
            Type = entity.Type,
            MetaValues = entity.MetaValues.ToResponse(),

            About = entity.About,
            AvatarUrl = entity.AvatarUrl,
        };
}
