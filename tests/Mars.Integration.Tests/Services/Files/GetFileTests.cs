using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Services.Files;

public class GetFileTests : ApplicationTests
{
    private readonly IFileService _fileService;
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly string _exampleFilesPath;


    public GetFileTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _fileService = appFixture.ServiceProvider.GetRequiredService<IFileService>();
        var opService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        _fileHostingInfo = opService.FileHostingInfo();
        _exampleFilesPath = Path.Join(Directory.GetCurrentDirectory(), "..\\..\\..", "Controllers\\Medias\\ExampleFiles\\");

    }

    [IntegrationFact]
    public async Task ListFile_Request_Success()
    {
        //Arrange
        _ = nameof(FileService.List);
        _ = nameof(FileRepository.List);
        FileEntity[] files = [_fixture.CreateImagePng(), _fixture.CreateImagePng()];

        using var ef = AppFixture.MarsDbContext();
        ef.Files.AddRange(files);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        //Act
        var result = await _fileService.List(new(), default);

        //Assert
        result.Items.Should().HaveCount(2);
    }
}
