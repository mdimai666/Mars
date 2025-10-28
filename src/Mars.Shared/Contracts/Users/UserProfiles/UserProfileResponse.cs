namespace Mars.Shared.Contracts.Users.UserProfiles;

public record UserProfileResponse : UserDetailResponse
{
    public required string? AvatarUrl { get; set; }
    public required string About { get; set; }
}
