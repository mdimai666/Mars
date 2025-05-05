namespace Mars.Host.Shared.WebSite.Models;

public record RenderParam
{
    public bool OnlyBody { get; init; }
    public bool AllowLayout { get; init; } = true;
}
