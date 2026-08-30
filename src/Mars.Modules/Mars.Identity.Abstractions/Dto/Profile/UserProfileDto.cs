using Mars.Identity.Abstractions.Dto.Users;

namespace Mars.Identity.Abstractions.Dto.Profile;

public record UserProfileDto : UserDetail
{
    public required string About { get; set; }
}
