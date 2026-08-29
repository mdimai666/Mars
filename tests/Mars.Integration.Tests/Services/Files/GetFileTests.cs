using FluentAssertions;
using Mars.Contracts.Dto.Files;
using Mars.Data.Entities;
using Mars.Data.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Media.Abstractions.Services;
using Mars.Options.Abstractions.Services;
using Mars.Test.Common.FixtureCustomizes;
using Mars.Test.Common.Helpers;
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
        _exampleFilesPath = SolutionPathHelper.Resolve("tests", "Mars.Integration.Tests", "Controllers", "Medias", "ExampleFiles");

    }

    [IntegrationFact]
    public async Task ListFile_Request_Success()
    {
        //Arrange
        _ = nameof(FileService.List);
        _ = nameof(FileRepository.List);
        FileEntity[] files = [_fixture.CreateImagePng(), _fixture.CreateImagePng()];

        var ef = AppFixture.MarsDbContext();
        ef.Files.AddRange(files);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        //Act
        var result = await _fileService.List(new(), default);

        //Assert
        result.Items.Should().HaveCount(2);
    }
}
