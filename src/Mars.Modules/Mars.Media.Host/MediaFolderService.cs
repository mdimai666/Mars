using Mars.Core.Exceptions;
using Mars.Contracts.Dto.Files;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;
using Mars.Media.Abstractions.Services;
using Mars.Media.Host.Services;
using Mars.Options.Services;
using Mars.Server.Abstractions.Services;
using Mars.Contracts.Dto.Files;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;
using Mars.Media.Abstractions.Services;
using Mars.Media.Host.Services;
using Mars.Options.Services;
using Mars.Server.Abstractions.Services;
using Mars.Contracts.Dto.Files;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;
using Mars.Media.Abstractions.Services;
using Mars.Media.Host.Services;
using Mars.Options.Services;
using Mars.Server.Abstractions.Services;
using Mars.Contracts.Common;

namespace Mars.Media.Host.Services;

internal class MediaFolderService : IMediaFolderService
{
    private readonly IMediaFolderRepository _folderRepository;
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorage _fileStorage;
    private readonly FileHostingInfo _hostingInfo;

    public MediaFolderService(
        IMediaFolderRepository folderRepository,
        IFileRepository fileRepository,
        IFileStorage fileStorage,
        IOptionService optionService)
    {
        _folderRepository = folderRepository;
        _fileRepository = fileRepository;
        _fileStorage = fileStorage;
        _hostingInfo = optionService.FileHostingInfo();
    }

    public Task<List<MediaFolderDto>> ListFolders(Guid? parentId, CancellationToken cancellationToken)
        => _folderRepository.ListByParent(parentId, cancellationToken);

    public Task<List<MediaFolderDto>> GetBreadcrumbs(Guid folderId, CancellationToken cancellationToken)
        => _folderRepository.GetBreadcrumbs(folderId, cancellationToken);

    public Task<MediaFolderDto?> GetById(Guid id, CancellationToken cancellationToken)
        => _folderRepository.GetById(id, cancellationToken);

    public async Task<MediaFolderDto> Create(CreateFolderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateFolderName(query.Name);

        if (await _folderRepository.ExistsByParentAndName(query.ParentId, query.Name, cancellationToken))
        {
            throw new UserActionException($"Папка с именем \"{query.Name}\" уже существует");
        }

        var parentDir = await GetParentDirPath(query.ParentId, cancellationToken);
        var path = parentDir + '/' + query.Name;

        if (!_fileStorage.DirectoryExists(path))
        {
            _fileStorage.CreateDirectory(path);
        }

        var id = await _folderRepository.Create(query with { Path = path }, cancellationToken);

        return await _folderRepository.GetById(id, cancellationToken) ?? throw new NotFoundException();
    }

    public async Task<MediaFolderDto> GetOrCreateByPath(string path, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _folderRepository.GetByPath(path, cancellationToken);
        if (existing is not null) return existing;

        var name = path[(path.LastIndexOf('/') + 1)..];
        var parentPath = path[..path.LastIndexOf('/')];

        Guid? parentId = null;
        if (parentPath != FileService.MediaDirName)
        {
            var parent = await GetOrCreateByPath(parentPath, userId, cancellationToken);
            parentId = parent.Id;
        }

        var id = await _folderRepository.Create(
            new CreateFolderQuery { ParentId = parentId, Name = name, UserId = userId, Path = path },
            cancellationToken);

        if (!_fileStorage.DirectoryExists(path))
        {
            _fileStorage.CreateDirectory(path);
        }

        return await _folderRepository.GetById(id, cancellationToken) ?? throw new NotFoundException();
    }

    public async Task<MediaFolderDto> Rename(Guid id, string newName, CancellationToken cancellationToken)
    {
        ValidateFolderName(newName);

        var folder = await _folderRepository.GetById(id, cancellationToken) ?? throw new NotFoundException();

        if (string.Equals(folder.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            return folder;
        }

        if (await _folderRepository.ExistsByParentAndName(folder.ParentId, newName, cancellationToken))
        {
            throw new UserActionException($"Папка с именем \"{newName}\" уже существует");
        }

        var parentDir = await GetParentDirPath(folder.ParentId, cancellationToken);
        var newPath = parentDir + '/' + newName;

        if (_fileStorage.DirectoryExists(folder.Path))
        {
            _fileStorage.MoveDirectory(folder.Path, newPath);
        }

        (string OldPrefix, string NewPrefix)? thumbPrefixes = null;
        var oldThumbDir = FileService.MediaThumbsDirName + '/' + folder.Path;
        var newThumbDir = FileService.MediaThumbsDirName + '/' + newPath;
        if (_fileStorage.DirectoryExists(oldThumbDir))
        {
            _fileStorage.MoveDirectory(oldThumbDir, newThumbDir);
            thumbPrefixes = (oldThumbDir, newThumbDir);
        }

        await _folderRepository.ApplyRename(id, newName, folder.Path, newPath, thumbPrefixes, _hostingInfo, cancellationToken);

        return await _folderRepository.GetById(id, cancellationToken) ?? throw new NotFoundException();
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var folder = await _folderRepository.GetById(id, cancellationToken) ?? throw new NotFoundException();

        if (await _folderRepository.HasChildren(id, cancellationToken))
        {
            throw new UserActionException("Нельзя удалить папку: есть вложенные папки");
        }
        if (await _fileRepository.CountByFolder(id, cancellationToken) > 0)
        {
            throw new UserActionException("Нельзя удалить папку: есть файлы");
        }

        if (_fileStorage.DirectoryExists(folder.Path))
        {
            var contents = _fileStorage.GetDirectoryContents(folder.Path);
            if (contents.Any())
            {
                throw new UserActionException("Нельзя удалить папку: физический каталог не пуст (файлы вне базы). Выполните «Сканировать файлы»");
            }

            _fileStorage.DeleteDirectory(folder.Path, true);
        }

        _fileStorage.DeleteDirectory(FileService.MediaThumbsDirName + '/' + folder.Path, true);

        await _folderRepository.Delete(id, cancellationToken);
    }

    public async Task<UserActionResult> MoveFiles(MoveFilesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Ids.Count == 0)
        {
            return new UserActionResult { Ok = true, Message = "Нет файлов для перемещения" };
        }

        MediaFolderDto? destFolder = null;
        if (query.FolderId is not null)
        {
            destFolder = await _folderRepository.GetById(query.FolderId.Value, cancellationToken)
                ?? throw new NotFoundException("Целевая папка не найдена");
        }

        var destDir = destFolder?.Path ?? FileService.MediaDirName;

        var files = await _fileRepository.ListAllDetail(new ListAllFileQuery { Ids = query.Ids }, _hostingInfo, cancellationToken);

        var updates = new List<FileMoveUpdate>(files.Count);

        foreach (var file in files)
        {
            var fileName = file.FilePhysicalPath[(file.FilePhysicalPath.LastIndexOf('/') + 1)..];
            var newPhysicalPath = destDir + '/' + fileName;

            if (newPhysicalPath == file.FilePhysicalPath) continue;

            if (_fileStorage.FileExists(newPhysicalPath))
            {
                throw new UserActionException($"В целевой папке уже есть файл с именем \"{fileName}\"");
            }

            _fileStorage.MoveFile(file.FilePhysicalPath, newPhysicalPath);

            FileEntityMetaDto? newMeta = null;
            if (file.Meta?.Thumbnails?.Count > 0)
            {
                var newThumbDir = FileService.MediaThumbsDirName + '/' + destDir;
                var thumbnails = new Dictionary<string, ImageThumbnailDto>(file.Meta.Thumbnails.Count);

                foreach (var kv in file.Meta.Thumbnails)
                {
                    var thumb = kv.Value;
                    var thumbFileName = thumb.FilePath[(thumb.FilePath.LastIndexOf('/') + 1)..];
                    var newThumbPath = newThumbDir + '/' + thumbFileName;

                    if (_fileStorage.FileExists(thumb.FilePath))
                    {
                        _fileStorage.MoveFile(thumb.FilePath, newThumbPath);
                    }

                    thumbnails[kv.Key] = thumb with
                    {
                        FilePath = newThumbPath,
                        FileUrl = _hostingInfo.FileRelativeUrlFromPath(newThumbPath),
                    };
                }

                newMeta = file.Meta with { Thumbnails = thumbnails };
            }

            updates.Add(new FileMoveUpdate
            {
                Id = file.Id,
                FilePhysicalPath = newPhysicalPath,
                FileVirtualPath = newPhysicalPath,
                FolderId = query.FolderId,
                Meta = newMeta,
            });
        }

        if (updates.Count > 0)
        {
            await _fileRepository.UpdateAfterMove(updates, _hostingInfo, cancellationToken);
        }

        return new UserActionResult
        {
            Ok = true,
            Message = $"Перемещено файлов: {updates.Count}",
        };
    }

    public async Task<Dictionary<string, Guid>> EnsureFoldersByPaths(IEnumerable<string> dirPaths, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _folderRepository.ListAll(cancellationToken);
        var pathToId = existing.ToDictionary(f => f.Path, f => f.Id);

        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in dirPaths)
        {
            var path = _hostingInfo.NormalizePathSlashes(dir);
            if (string.IsNullOrEmpty(path)) continue;

            // сам каталог и все предки до корня Media (не включая его)
            for (var p = path; p.Contains('/'); p = p[..p.LastIndexOf('/')])
            {
                if (p == FileService.MediaDirName) break;
                needed.Add(p);
            }
        }

        // сначала верхние уровни — чтобы родитель уже был в карте
        foreach (var path in needed.OrderBy(p => p.Count(c => c == '/')).ThenBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (pathToId.ContainsKey(path)) continue;

            var name = path[(path.LastIndexOf('/') + 1)..];
            var parentPath = path[..path.LastIndexOf('/')];
            Guid? parentId = parentPath == FileService.MediaDirName ? null : pathToId[parentPath];

            var id = await _folderRepository.Create(
                new CreateFolderQuery { ParentId = parentId, Name = name, UserId = userId, Path = path },
                cancellationToken);
            pathToId[path] = id;
        }

        return pathToId;
    }

    async Task<string> GetParentDirPath(Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null) return FileService.MediaDirName;

        var parent = await _folderRepository.GetById(parentId.Value, cancellationToken)
            ?? throw new NotFoundException("Родительская папка не найдена");

        return parent.Path;
    }

    internal static void ValidateFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new UserActionException("Имя папки не может быть пустым");
        }
        if (name.Length > FileService.MaxFileNameSize)
        {
            throw new UserActionException($"Имя папки не может быть длиннее {FileService.MaxFileNameSize} символов");
        }
        if (name.Contains('/') || name.Contains('\\') || name == "." || name == "..")
        {
            throw new UserActionException("Имя папки содержит недопустимые символы");
        }
    }
}
