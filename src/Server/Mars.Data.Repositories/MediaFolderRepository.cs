using Mars.Contracts.Dto.Files;
using Mars.Core.Exceptions;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.OwnedTypes.Files;
using Mars.Data.Repositories.Mappings;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Repositories;

internal class MediaFolderRepository : IMediaFolderRepository
{
    private readonly MarsDbContext _marsDbContext;

    public MediaFolderRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<List<MediaFolderDto>> ListByParent(Guid? parentId, CancellationToken cancellationToken)
    {
        var folders = await _marsDbContext.MediaFolders.AsNoTracking()
            .Where(f => f.ParentId == parentId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var counts = await GetFilesCounts(folders.Select(f => f.Id), cancellationToken);

        return folders.Select(f => f.ToDto(counts.GetValueOrDefault(f.Id))).ToList();
    }

    public async Task<List<MediaFolderDto>> ListAll(CancellationToken cancellationToken)
    {
        var folders = await _marsDbContext.MediaFolders.AsNoTracking()
            .OrderBy(f => f.Path)
            .ToListAsync(cancellationToken);

        var counts = await GetFilesCounts(folders.Select(f => f.Id), cancellationToken);

        return folders.Select(f => f.ToDto(counts.GetValueOrDefault(f.Id))).ToList();
    }

    public async Task<List<MediaFolderDto>> GetBreadcrumbs(Guid folderId, CancellationToken cancellationToken)
    {
        var all = await _marsDbContext.MediaFolders.AsNoTracking().ToListAsync(cancellationToken);

        var byId = all.ToDictionary(f => f.Id);
        if (!byId.TryGetValue(folderId, out var cursor)) return [];

        var chain = new List<MediaFolderEntity>();
        while (cursor is not null)
        {
            chain.Add(cursor);
            cursor = cursor.ParentId is Guid parentId && byId.TryGetValue(parentId, out var parent) ? parent : null;
        }

        chain.Reverse();
        return chain.Select(f => f.ToDto()).ToList();
    }

    public async Task<MediaFolderDto?> GetById(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.MediaFolders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken))?.ToDto();

    public async Task<MediaFolderDto?> GetByPath(string path, CancellationToken cancellationToken)
        => (await _marsDbContext.MediaFolders.AsNoTracking().FirstOrDefaultAsync(f => f.Path == path, cancellationToken))?.ToDto();

    public Task<bool> ExistsByParentAndName(Guid? parentId, string name, CancellationToken cancellationToken)
        => _marsDbContext.MediaFolders.AsNoTracking()
            .AnyAsync(f => f.ParentId == parentId && EF.Functions.ILike(f.Name, name), cancellationToken);

    public Task<bool> HasChildren(Guid id, CancellationToken cancellationToken)
        => _marsDbContext.MediaFolders.AsNoTracking().AnyAsync(f => f.ParentId == id, cancellationToken);

    public async Task<Guid> Create(CreateFolderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        if (string.IsNullOrEmpty(query.Path)) throw new ArgumentException("Path is required", nameof(query));

        var entity = new MediaFolderEntity
        {
            Name = query.Name,
            Path = query.Path,
            ParentId = query.ParentId,
            CreatedBy = query.UserId,
        };

        await _marsDbContext.MediaFolders.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _marsDbContext.MediaFolders.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyRename(
        Guid folderId,
        string newName,
        string oldPath,
        string newPath,
        (string OldPrefix, string NewPrefix)? thumbPrefixes,
        FileHostingInfo hostingInfo,
        CancellationToken cancellationToken)
    {
        var modifiedAt = DateTimeOffset.Now;
        var oldPrefix = oldPath + '/';

        var folders = await _marsDbContext.MediaFolders.ToListAsync(cancellationToken);
        foreach (var folder in folders)
        {
            if (folder.Id == folderId)
            {
                folder.Name = newName;
                folder.Path = newPath;
                folder.ModifiedAt = modifiedAt;
            }
            else if (folder.Path.StartsWith(oldPrefix))
            {
                folder.Path = newPath + folder.Path[oldPath.Length..];
                folder.ModifiedAt = modifiedAt;
            }
        }

        var files = await _marsDbContext.Files
            .Where(s => s.FilePhysicalPath.StartsWith(oldPrefix))
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            file.FilePhysicalPath = newPath + file.FilePhysicalPath[oldPath.Length..];

            if (file.FileVirtualPath.StartsWith(oldPrefix))
            {
                file.FileVirtualPath = newPath + file.FileVirtualPath[oldPath.Length..];
            }

            if (thumbPrefixes is { } thumbs && file.Meta?.Thumbnails?.Count > 0)
            {
                var newThumbnails = file.Meta.Thumbnails.ToDictionary(
                    kv => kv.Key,
                    kv => RewriteThumbPath(kv.Value, thumbs.OldPrefix, thumbs.NewPrefix, hostingInfo));

                file.Meta = new FileEntityMeta
                {
                    ImageInfo = file.Meta.ImageInfo,
                    Thumbnails = newThumbnails,
                };
            }

            file.ModifiedAt = modifiedAt;
        }

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    static ImageThumbnail RewriteThumbPath(ImageThumbnail thumb, string oldPrefix, string newPrefix, FileHostingInfo hostingInfo)
    {
        if (!thumb.FilePath.StartsWith(oldPrefix)) return thumb;

        var newFilePath = newPrefix + thumb.FilePath[oldPrefix.Length..];
        return new ImageThumbnail
        {
            Name = thumb.Name,
            Width = thumb.Width,
            Height = thumb.Height,
            FilePath = newFilePath,
            FileUrl = hostingInfo.FileRelativeUrlFromPath(newFilePath),
        };
    }

    async Task<Dictionary<Guid, int>> GetFilesCounts(IEnumerable<Guid> folderIds, CancellationToken cancellationToken)
    {
        var ids = folderIds.ToList();
        if (ids.Count == 0) return [];

        return await _marsDbContext.Files.AsNoTracking()
            .Where(s => s.FolderId != null && ids.Contains(s.FolderId.Value))
            .GroupBy(s => s.FolderId!.Value)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FolderId, x => x.Count, cancellationToken);
    }
}
