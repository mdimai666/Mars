using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.Shared.Dto.Profile;

public record UserProfileDto : UserDetail
{
    public required string About { get; set; }
}
