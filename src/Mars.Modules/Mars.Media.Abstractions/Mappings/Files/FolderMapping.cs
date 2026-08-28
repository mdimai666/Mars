using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Contracts.Files;

namespace Mars.Media.Abstractions.Mappings.Files;

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
