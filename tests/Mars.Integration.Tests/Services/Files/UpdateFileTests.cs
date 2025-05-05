using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Services.Files;

public class UpdateFileTests : ApplicationTests
{
    private readonly IFileService _fileService;
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly string _exampleFilesPath;


    public UpdateFileTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fileService = appFixture.ServiceProvider.GetRequiredService<IFileService>();
        var opService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        _fileHostingInfo = opService.FileHostingInfo();
        _exampleFilesPath = Path.Join(Directory.GetCurrentDirectory(), "..\\..\\..", "Controllers\\Medias\\ExampleFiles\\");

    }

    [IntegrationFact]
    public async Task UpdateFile_Request_Success()
    {
        //Arrange
        _ = nameof(FileService.GetDetail);
        _ = nameof(FileRepository.Get);
        FileEntity file = _fixture.CreateImagePng();

        using var ef = AppFixture.MarsDbContext();
        ef.Files.Add(file);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        var query = new UpdateFileQuery
        {
            Id = file.Id,
            Name = "111" + file.FileName,
            Meta = file.Meta.ToDto(_fileHostingInfo),
        };

        //Act
        var action = () => _fileService.Update(query, _fileHostingInfo, default);

        //Assert
        await action.Should().NotThrowAsync();
        var dbFile = await _fileService.GetDetail(file.Id, default);
        dbFile.Name.Should().Be(query.Name);
        dbFile.Meta.Should().BeEquivalentTo(query.Meta, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<FileEntityMetaDto>()
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task UpdateBulkFile_Request_Success()
    {
        //Arrange
        _ = nameof(FileService.GetDetail);
        _ = nameof(FileRepository.Get);
        FileEntity[] files = [_fixture.CreateImagePng(), _fixture.CreateImagePng()];

        using var ef = AppFixture.MarsDbContext();
        ef.Files.AddRange(files);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        var file = files.First();
        var query = new UpdateFileQuery
        {
            Id = file.Id,
            Name = "111" + file.FileName,
            Meta = file.Meta.ToDto(_fileHostingInfo),
        };

        //Act
        var action = () => _fileService.UpdateBulk([query], _fileHostingInfo, default);

        //Assert
        await action.Should().NotThrowAsync();
        var dbFile = await _fileService.GetDetail(file.Id, default);
        dbFile.Name.Should().Be(query.Name);
        dbFile.Meta.Should().BeEquivalentTo(query.Meta, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<FileEntityMetaDto>()
            .ExcludingMissingMembers());
    }
}
