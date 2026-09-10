using Mars.Contracts.Common;

namespace Mars.Media.Contracts.Files;

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
    /// <summary>
    /// Фильтр по папке: null — все файлы, Guid.Empty — корень (файлы без папки), иначе — указанная папка.
    /// </summary>
    public Guid? FolderId { get; init; }
}

public record TableFileQueryRequest : BasicTableQueryRequest
{
    /// <summary>
    /// Фильтр по папке: null — все файлы, Guid.Empty — корень (файлы без папки), иначе — указанная папка.
    /// </summary>
    public Guid? FolderId { get; init; }
}

public record CreateFolderRequest
{
    public required string Name { get; init; }
    public Guid? ParentId { get; init; }
}

public record RenameFolderRequest
{
    public required string NewName { get; init; }
}

public record MoveFilesRequest
{
    public required Guid[] Ids { get; init; }

    /// <summary>
    /// Целевая папка. null — переместить в корень.
    /// </summary>
    public Guid? FolderId { get; init; }
}
