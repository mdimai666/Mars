using Mars.Data.Entities;
using Mars.Media.Abstractions.Dto.Files;

namespace Mars.Data.Repositories.Mappings;

internal static class MediaFolderMapping
{
    public static MediaFolderDto ToDto(this MediaFolderEntity entity, int filesCount = 0)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Name = entity.Name,
            Path = entity.Path,
            ParentId = entity.ParentId,
            CreatedBy = entity.CreatedBy,
            Icon = entity.Icon,
            FilesCount = filesCount,
        };
}
