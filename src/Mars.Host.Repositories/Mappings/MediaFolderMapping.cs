using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Files;

namespace Mars.Host.Repositories.Mappings;

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
