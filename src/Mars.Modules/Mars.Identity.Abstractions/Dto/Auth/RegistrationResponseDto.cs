namespace Mars.Host.Shared.Dto.Auth;

public class RegistrationResponseDto
{
    public bool IsSuccessfulRegistration { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = [];
    public required int Code { get; init; }
}
