namespace Mars.Shared.Contracts.PostTypes;

public record PostTypeModelInfoResponse
{
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required bool IsPlugin { get; set; }

    public IReadOnlyCollection<PostTypeModelSubTypeInfoResponse>? SubTypes;

}

public record PostTypeModelSubTypeInfoResponse
{
    public required string Name { get; set; }
    public required string Title { get; set; }
}
