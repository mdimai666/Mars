using System.Text;
using FluentAssertions;
using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Media.Contracts.Files;
using Mars.Media.Host.Controllers;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Medias;

/// <seealso cref="MediaController"/>
public sealed class MediaFolderTests : ApplicationTests
{
    private const string _apiUrl = "/api/Media";
    private readonly IFileStorage _fileStorage;

    public MediaFolderTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fileStorage = AppFixture.ServiceProvider.GetRequiredService<IFileStorage>();
    }

    [IntegrationFact]
    public async Task CreateFolder_ValidRequest_ShouldCreateDbRecordAndPhysicalDirectory()
    {
        //Arrange
        _ = nameof(MediaController.CreateFolder);
        var client = AppFixture.GetClient();

        //Act
        var folder = await CreateFolder(client, "test-folder");

        //Assert
        folder.Name.Should().Be("test-folder");
        folder.Path.Should().Be("Media/test-folder");
        folder.ParentId.Should().BeNull();

        var ef = AppFixture.MarsDbContext();
        ef.MediaFolders.Any(f => f.Id == folder.Id).Should().BeTrue();

        _fileStorage.DirectoryExists("Media/test-folder").Should().BeTrue();

        _fileStorage.DeleteDirectory("Media/test-folder", true);
    }

    [IntegrationFact]
    public async Task ListFolders_ReturnsCreatedFoldersWithFilesCount()
    {
        //Arrange
        _ = nameof(MediaController.ListFolders);
        var client = AppFixture.GetClient();
        var folderA = await CreateFolder(client, "folder-a");
        var folderB = await CreateFolder(client, "folder-b");
        await UploadTextFile(client, "file1.txt", folderA.Id);

        //Act
        var response = await client.Request(_apiUrl, "folders")
                                .GetAsync()
                                .CatchUserActionError();
        var folders = await response.GetJsonAsync<List<FolderResponse>>();

        //Assert
        folders.Should().Contain(f => f.Id == folderA.Id && f.FilesCount == 1);
        folders.Should().Contain(f => f.Id == folderB.Id && f.FilesCount == 0);

        CleanupFolders(folderA.Path, folderB.Path);
    }

    [IntegrationFact]
    public async Task Upload_WithFolderId_FileGoesIntoFolder()
    {
        //Arrange
        _ = nameof(MediaController.Upload);
        var client = AppFixture.GetClient();
        var folder = await CreateFolder(client, "upload-folder");

        //Act
        var file = await UploadTextFile(client, "file1.txt", folder.Id);

        //Assert
        file.FilePhysicalPath.Should().StartWith("Media/upload-folder/");

        var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.First(s => s.Id == file.Id);
        dbFile.FolderId.Should().Be(folder.Id);
        _fileStorage.FileExists(dbFile.FilePhysicalPath).Should().BeTrue();

        CleanupFolders(folder.Path);
    }

    [IntegrationFact]
    public async Task ListTable_FilterByFolder_ReturnsOnlyFolderFiles()
    {
        //Arrange
        _ = nameof(MediaController.ListTable);
        var client = AppFixture.GetClient();
        var folderA = await CreateFolder(client, "filter-a");
        var folderB = await CreateFolder(client, "filter-b");
        await UploadTextFile(client, "file-a.txt", folderA.Id);
        await UploadTextFile(client, "file-b.txt", folderB.Id);

        //Act
        var result = await client.Request(_apiUrl, "list/page")
                                .AppendQueryParam(new TableFileQueryRequest { FolderId = folderA.Id })
                                .GetJsonAsync<PagingResult<FileListItemResponse>>();

        //Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(f => f.FolderId == folderA.Id);
        result.Items.First().Name.Should().Be("file-a.txt");

        CleanupFolders(folderA.Path, folderB.Path);
    }

    [IntegrationFact]
    public async Task RenameFolder_ShouldMovePhysicalDirectoryAndRewritePaths()
    {
        //Arrange
        _ = nameof(MediaController.RenameFolder);
        var client = AppFixture.GetClient();
        var folder = await CreateFolder(client, "old-name");
        var file = await UploadTextFile(client, "file1.txt", folder.Id);

        //Act
        var renamed = await client.Request(_apiUrl, "folders", folder.Id, "rename")
                                .PutJsonAsync(new RenameFolderRequest { NewName = "new-name" })
                                .CatchUserActionError()
                                .ReceiveJson<FolderResponse>();

        //Assert
        renamed.Path.Should().Be("Media/new-name");

        var ef = AppFixture.MarsDbContext();
        var dbFolder = ef.MediaFolders.First(f => f.Id == folder.Id);
        dbFolder.Name.Should().Be("new-name");
        dbFolder.Path.Should().Be("Media/new-name");

        var dbFile = ef.Files.First(s => s.Id == file.Id);
        dbFile.FilePhysicalPath.Should().StartWith("Media/new-name/");
        dbFile.FolderId.Should().Be(folder.Id);

        _fileStorage.DirectoryExists("Media/new-name").Should().BeTrue();
        _fileStorage.DirectoryExists("Media/old-name").Should().BeFalse();
        _fileStorage.FileExists(dbFile.FilePhysicalPath).Should().BeTrue();

        CleanupFolders("Media/new-name");
    }

    [IntegrationFact]
    public async Task MoveFiles_ToAnotherFolder_ShouldMoveFileAndUpdateFolderId()
    {
        //Arrange
        _ = nameof(MediaController.MoveFiles);
        var client = AppFixture.GetClient();
        var folderA = await CreateFolder(client, "move-from");
        var folderB = await CreateFolder(client, "move-to");
        var file = await UploadTextFile(client, "file1.txt", folderA.Id);

        //Act
        var result = await client.Request(_apiUrl, "move-files")
                                .PostJsonAsync(new MoveFilesRequest { Ids = [file.Id], FolderId = folderB.Id })
                                .CatchUserActionError()
                                .ReceiveJson<UserActionResult>();

        //Assert
        result.Ok.Should().BeTrue();

        var ef = AppFixture.MarsDbContext();
        var dbFile = ef.Files.First(s => s.Id == file.Id);
        dbFile.FolderId.Should().Be(folderB.Id);
        dbFile.FilePhysicalPath.Should().StartWith("Media/move-to/");

        _fileStorage.FileExists(dbFile.FilePhysicalPath).Should().BeTrue();
        _fileStorage.FileExists(file.FilePhysicalPath).Should().BeFalse();

        CleanupFolders(folderA.Path, folderB.Path);
    }

    [IntegrationFact]
    public async Task DeleteFolder_EmptyFolder_ShouldDelete()
    {
        //Arrange
        _ = nameof(MediaController.DeleteFolder);
        var client = AppFixture.GetClient();
        var folder = await CreateFolder(client, "to-delete");

        //Act
        await client.Request(_apiUrl, "folders", folder.Id)
                    .DeleteAsync()
                    .CatchUserActionError();

        //Assert
        var ef = AppFixture.MarsDbContext();
        ef.MediaFolders.Any(f => f.Id == folder.Id).Should().BeFalse();
        _fileStorage.DirectoryExists("Media/to-delete").Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeleteFolder_FolderWithFiles_ShouldError466()
    {
        //Arrange
        _ = nameof(MediaController.DeleteFolder);
        var client = AppFixture.GetClient();
        var folder = await CreateFolder(client, "not-empty");
        _ = await UploadTextFile(client, "file1.txt", folder.Id);

        //Act
        var response = await client.Request(_apiUrl, "folders", folder.Id)
                                    .AllowAnyHttpStatus()
                                    .DeleteAsync();

        //Assert
        response.StatusCode.Should().Be(HttpConstants.UserActionErrorCode466);
        var error = await response.GetJsonAsync<UserActionResult>();
        error.Ok.Should().BeFalse();

        var ef = AppFixture.MarsDbContext();
        ef.MediaFolders.Any(f => f.Id == folder.Id).Should().BeTrue();

        CleanupFolders(folder.Path);
    }

    //////////////// helpers

    Task<FolderResponse> CreateFolder(IFlurlClient client, string name, Guid? parentId = null)
        => client.Request(_apiUrl, "folders")
                 .PostJsonAsync(new CreateFolderRequest { Name = name, ParentId = parentId })
                 .CatchUserActionError()
                 .ReceiveJson<FolderResponse>();

    Task<FileDetailResponse> UploadTextFile(IFlurlClient client, string fileName, Guid? folderId = null)
    {
        var request = client.Request(_apiUrl, "Upload");
        if (folderId is not null)
        {
            request = request.AppendQueryParam("folderId", folderId.Value.ToString());
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("TEST-text"));
        return request.PostMultipartAsync(mp => mp.AddFile("file", stream, fileName))
                      .CatchUserActionError()
                      .ReceiveJson<FileDetailResponse>();
    }

    void CleanupFolders(params string[] paths)
    {
        foreach (var path in paths)
        {
            _fileStorage.DeleteDirectory(path, true);
            _fileStorage.DeleteDirectory("MediaThumbs/" + path, true);
        }
    }
}
