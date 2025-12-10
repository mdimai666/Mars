using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.PostJsons;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostJsons;

public class UpdatePostJsonTests : ApplicationTests
{
    const string _apiUrl = "/api/PostJson";

    public UpdatePostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdatePostJson_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostJsonController.Update);
        _ = nameof(PostJsonService.Update);
        var client = AppFixture.GetClient(true);

        var postRequest = _fixture.Create<UpdatePostJsonRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(postRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdatePostJson_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.Update);
        _ = nameof(PostJsonService.Update);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();
        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        var metaValues = metaFields.ConvertAll(mf =>
        {
            var mv = _fixture.MetaValueEntity(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        });
        createdPost.MetaValues = metaValues;
        ef.Posts.Add(createdPost);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValuesQuery = metaValues.Select(s => ModifyMetaValueDetailQuery.GetBlank(s.MetaField!.ToDto(), s.Id).SetMetaValue(_fixture)).ToList();
        var metaValuesUpdateList = metaValuesQuery.ToDictionary(s => s.MetaField.Key, s => JsonValue.Create(s.GetValueSimple()));

        var post = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = createdPost.Id,
            Meta = metaValuesUpdateList!
        };

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<PostJsonResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();
        var dbPost = ef.Posts.Include(s => s.MetaValues!)
                                .ThenInclude(s => s.MetaField)
                                .FirstOrDefault(s => s.Id == post.Id);
        dbPost.Should().NotBeNull();

        dbPost.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostJsonRequest>()
            .Excluding(s => s.Meta)
            .ExcludingMissingMembers());
        dbPost.MetaValues.Count.Should().Be(metaValuesQuery.Count);
        dbPost.MetaValues.Should().AllSatisfy(e =>
        {
            var expectValue = metaValuesQuery.First(s => s.MetaField.Key == e.MetaField.Key && s.Id == e.Id);
            var current = e.Get();
            var expect = expectValue.GetValueSimple();
            current.Should().BeEquivalentTo(expect);

            e.ToDto().Should().BeEquivalentTo(expectValue, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<MetaValueDetailBase>()
                .Excluding(s => s.Id)
                .Excluding(s => s.MetaField)
                .ExcludingMissingMembers());
        });
    }

    [IntegrationFact]
    public async Task UpdatePostJson_UpdateNonWrittedValueMetaValueRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.Update);
        _ = nameof(PostJsonService.Update);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var createdPost = _fixture.Create<PostEntity>();
        ef.Posts.Add(createdPost);
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValues = metaFields.ConvertAll(mf =>
        {
            var mv = _fixture.MetaValueEntity(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        });
        var metaValuesQuery = metaValues.Select(s => ModifyMetaValueDetailQuery.GetBlank(s.MetaField!.ToDto(), s.Id).SetMetaValue(_fixture)).ToList();
        var metaValuesUpdateList = metaValuesQuery.ToDictionary(s => s.MetaField.Key, s => JsonValue.Create(s.GetValueSimple()));

        var post = _fixture.Create<UpdatePostJsonRequest>() with
        {
            Id = createdPost.Id,
            Meta = metaValuesUpdateList!
        };

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<PostJsonResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();
        var dbPost = ef.Posts.Include(s => s.MetaValues!)
                                .ThenInclude(s => s.MetaField)
                                .FirstOrDefault(s => s.Id == post.Id);
        dbPost.Should().NotBeNull();

        dbPost.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdatePostJsonRequest>()
            .Excluding(s => s.Meta)
            .ExcludingMissingMembers());
        dbPost.MetaValues.Count.Should().Be(metaValuesQuery.Count);
        dbPost.MetaValues.Should().AllSatisfy(e =>
        {
            var expectValue = metaValuesQuery.First(s => s.MetaField.Key == e.MetaField.Key);
            var current = e.Get();
            var expect = expectValue.GetValueSimple();
            current.Should().BeEquivalentTo(expect);

            e.ToDto().Should().BeEquivalentTo(expectValue, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<MetaValueDetailBase>()
                .Excluding(s => s.Id)
                .Excluding(s => s.MetaField)
                .ExcludingMissingMembers());
        });
    }

    [IntegrationFact]
    public async Task UpdatePostJson_ValidateQueryValidator_ShouldFail()
    {
        //Arrange
        _ = nameof(PostRepository.Update);
        _ = nameof(PostJsonController.Update);
        _ = nameof(UpdatePostJsonQueryValidator);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();
        var ef = AppFixture.MarsDbContext();
        ef.Posts.Add(createdPost);
        ef.SaveChanges();

        var request = _fixture.Create<UpdatePostJsonRequest>() with { Status = "invalid_status_name", Id = createdPost.Id };

        //Act
        var validate = await client.Request(_apiUrl).PutJsonAsync(request).ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(UpdatePostJsonRequest.Status));
    }
}
