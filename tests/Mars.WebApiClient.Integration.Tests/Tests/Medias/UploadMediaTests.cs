using System.Text;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Medias;

public class UploadMediaTests : BaseWebApiClientTests
{
    public UploadMediaTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Upload_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Media.Upload(GenerateStreamFromString("xxx"), "text1.txt");

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task Upload_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var filename = "text1.txt";

        //Act
        var result = await client.Media.Upload(GenerateStreamFromString("xxx"), filename);

        //Assert
        result.Name.Should().Be(filename);
        var file = AppFixture.MarsDbContext().Files.FirstOrDefault(s => s.Id == result.Id);
        file.Should().NotBeNull();
        file.FileName.Should().Be(filename);
        file.FileSize.Should().Be(3);
    }

    private MemoryStream GenerateStreamFromString(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
    }
}
