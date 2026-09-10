namespace Mars.Identity.Contracts.Users.UserProfiles;

public record UserProfileResponse : UserDetailResponse
{
    public required string About { get; set; }
}
