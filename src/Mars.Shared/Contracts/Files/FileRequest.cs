using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Files;

public record CreateFileRequest
{
    public required string Name { get; init; }
}

public record UpdateFileRequest
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

public record ListFileQueryRequest : BasicListQueryRequest
{

}

public record TableFileQueryRequest : BasicTableQueryRequest
{

}
