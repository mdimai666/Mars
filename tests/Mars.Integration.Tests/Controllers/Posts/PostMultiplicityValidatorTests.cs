using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>
/// Кратность мета-поля (колонка <c>IsMultiple</c>): одинарное поле отклоняет
/// значения с <c>Index &gt; 0</c>, множественное принимает несколько строк
/// </summary>
public sealed class PostMultiplicityValidatorTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public PostMultiplicityValidatorTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    async Task<(string TypeName, Guid FieldId)> CreateTypeWithFieldAsync(bool isMultiple)
    {
        var field = _fixture.Create<CreateMetaFieldRequest>() with
        {
            Type = MetaFieldType.String,
            MinValue = null,
            MaxValue = null,
            Options = null,
            IsMultiple = isMultiple,
        };

        var typeRequest = _fixture.Create<CreatePostTypeRequest>() with { MetaFields = [field] };
        var postType = await AppFixture.ServiceProvider.GetRequiredService<IPostTypeService>()
                                       .Create(typeRequest.ToQuery(), default);

        return (postType.TypeName, field.Id);
    }

    CreateMetaValueRequest Value(Guid fieldId, int index)
        => _fixture.CreateSimpleCreateMetaValueRequest(fieldId, EMetaFieldType.String) with { Index = index };

    CreatePostRequest PostRequest(string typeName, Guid fieldId, params CreateMetaValueRequest[] values)
        => _fixture.Create<CreatePostRequest>() with
        {
            Type = typeName,
            Status = null,
            CategoryIds = [],
            MetaValues = values,
        };

    [IntegrationFact]
    public async Task CreatePost_SecondValueForSingleField_Fails400()
    {
        //Arrange
        var (typeName, fieldId) = await CreateTypeWithFieldAsync(isMultiple: false);
        var client = AppFixture.GetClient();

        //Act
        var validate = await client.Request(_apiUrl)
            .PostJsonAsync(PostRequest(typeName, fieldId, Value(fieldId, 0), Value(fieldId, 1)))
            .ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("MetaValues");
        validate.Errors["MetaValues"].Should().Contain(m => m.Contains("только одно значение"));
    }

    [IntegrationFact]
    public async Task CreatePost_SeveralValuesForMultipleField_Succeeds()
    {
        //Arrange
        var (typeName, fieldId) = await CreateTypeWithFieldAsync(isMultiple: true);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl)
            .PostJsonAsync(PostRequest(typeName, fieldId, Value(fieldId, 0), Value(fieldId, 1)))
            .CatchUserActionError();
        var result = await res.GetJsonAsync<PostDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.MetaValues.Should().ContainSingle();
        result.MetaValues.Values.Single().Should().HaveCount(2);

        var ef = AppFixture.MarsDbContext();
        ef.PostMetaValues.AsNoTracking().Count(v => v.PostId == result.Id && v.MetaFieldId == fieldId)
          .Should().Be(2);
    }
}
