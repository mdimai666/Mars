namespace Mars.Host.Shared.Dto.Files;

public record ListAllFileQuery
{
    public bool? IsImage { get; init; }
    public IReadOnlyCollection<Guid>? Ids { get; init; }
}
