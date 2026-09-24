namespace Mars.Docker.Contracts;

public record PullImageRequest
{
    /// <summary>Имя образа без тега, например <c>alpine</c> или <c>ghcr.io/owner/app</c>.</summary>
    public required string Image { get; init; }

    /// <summary>Тег; пустой — <c>latest</c>.</summary>
    public string Tag { get; init; } = "latest";
}
