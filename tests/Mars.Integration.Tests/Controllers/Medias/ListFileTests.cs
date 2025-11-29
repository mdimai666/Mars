using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Medias;

/// <seealso cref="Mars.Controllers.MediaController"/>
public sealed class ListFileTests : ApplicationTests
{
    const string _apiUrl = "/api/Media";

    public ListFileTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task ListFiles_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(MediaController.List);
        var client = AppFixture.GetClient(true);
        var listFileRequest = new ListFileQueryRequest();

        //Act
        var result = await client.Request(_apiUrl).AppendQueryParam(listFileRequest).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListFiles_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MediaController.List);
        var client = AppFixture.GetClient();
        var listFileRequest = new ListFileQueryRequest();

        var ef = AppFixture.MarsDbContext();
        var fileEntities = _fixture.CreateMany<FileEntity>(2);
        await ef.Files.AddRangeAsync(fileEntities);
        await ef.SaveChangesAsync();

        //Act
        var result = await client.Request(_apiUrl).GetJsonAsync<ListDataResult<FileListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
    }
}
