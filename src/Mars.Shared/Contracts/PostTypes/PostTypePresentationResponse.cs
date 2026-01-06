using Mars.Shared.Models;

namespace Mars.Shared.Contracts.PostTypes;

public record PostTypePresentationResponse
{
    public required SourceUri? ListViewTemplate { get; init; }
}
