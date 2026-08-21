using Mars.Host.Shared.Dto.Files;
using Mars.Shared.Contracts.Files;

namespace Mars.Host.Shared.Mappings.Files;

public static class FolderMapping
{
    public static FolderResponse ToResponse(this MediaFolderDto folder)
        => new()
        {
            Id = folder.Id,
            CreatedAt = folder.CreatedAt,
            Name = folder.Name,
            Path = folder.Path,
            ParentId = folder.ParentId,
            CreatedBy = folder.CreatedBy,
            Icon = folder.Icon,
            FilesCount = folder.FilesCount,
        };

    public static List<FolderResponse> ToResponseList(this IReadOnlyCollection<MediaFolderDto> folders)
        => folders.Select(ToResponse).ToList();
}
