using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostTypes;

public class UpdatePostTypePresentationTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType/presentation";

    public UpdatePostTypePresentationTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostTypePresentation_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.UpdatePresentation);
        _ = nameof(PostTypeRepository.UpdatePresentation);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = "templates2";

        var templatePost = _fixture.Create<PostEntity>();
        templatePost.PostType = postType;
        templatePost.Slug = "editView2";

        ef.PostTypes.Add(postType);
        ef.Posts.Add(templatePost);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        var updatingId = postType.Id;
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var request = new UpdatePostTypePresentationQuery
        {
            Id = updatingId,
            ListViewTemplate = $"/{postType.TypeName}/{templatePost.Slug}"
        };

        //Act
        var result = await client.Request(_apiUrl, "update").PutJsonAsync(request).CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var postTypeEntity = ef.PostTypes.Include(s => s.Presentation).First(s => s.Id == updatingId);
        postTypeEntity.Presentation.Should().BeEquivalentTo(request, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostTypePresentationQuery>()
            .ExcludingMissingMembers());
    }
}
