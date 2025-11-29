using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <seealso cref="Mars.Controllers.PostController"/>
public sealed class UpdatePostTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public UpdatePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePost_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.Update);
        _ = nameof(PostRepository.Update);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = new(metaFields);
        var metaValues = metaFields.Select(mf =>
        {
            var mv = _fixture.MetaValueEntity(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        }).ToList();
        createdPost.MetaValues = metaValues;
        ef.Posts.Add(createdPost);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValueUpdates = metaValues.Select((mv, i) => _fixture.UpdateSimpleCreateMetaValueRequest(i != 0 ? mv.Id : Guid.NewGuid(), mv.MetaField.Id, mv.MetaField.Type)).ToArray();

        var post = _fixture.Create<UpdatePostRequest>() with
        {
            Id = createdPost.Id,
            Type = "post",
            MetaValues = metaValueUpdates
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbPost = ef.Posts.Include(s => s.MetaValues).FirstOrDefault(s => s.Id == post.Id);
        dbPost.Should().NotBeNull();

        dbPost.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostRequest>()
            .Excluding(s => s.MetaValues)
            .ExcludingMissingMembers());

        dbPost.MetaValues.Should().AllSatisfy(e =>
        {
            var req = post.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<UpdateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });
    }

    [IntegrationFact]
    public async Task UpdatePost_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(PostController.Update);
        _ = nameof(PostService.Update);
        var client = AppFixture.GetClient();

        var updatePostRequest = _fixture.Create<UpdatePostRequest>();
        updatePostRequest = updatePostRequest with
        {
            //Title = string.Empty,
            Type = "invalid_type",
        };

        var expectError = new Dictionary<string, string[]>()
        {
            //[nameof(UpdatePostRequest.Title)] = ["*zu*"],
            [nameof(UpdatePostRequest.Type)] = ["*exist*"],
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updatePostRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.Should().HaveSameCount(expectError);
        result.Errors.Should().AllSatisfy(x =>
        {
            foreach (var pattern in expectError[x.Key])
            {
                x.Value.Should().ContainMatch(pattern);
            }
            //expectError[x.Key].Should().ContainMatch(x.Value); //order insensetive
        });
    }
}
