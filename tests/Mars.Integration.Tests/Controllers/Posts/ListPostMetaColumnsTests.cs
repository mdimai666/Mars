using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Data.OwnedTypes.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Contracts.Common;
using Mars.Cms.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>Список постов с приложенными значениями мета-полей (колонки грида)</summary>
public class ListPostMetaColumnsTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public ListPostMetaColumnsTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task ListPost_WithMetaFields_ShouldReturnFormattedMetaColumns()
    {
        //Arrange
        _ = nameof(PostController.List);
        _ = nameof(PostMetaColumnsService);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var user = await ef.Users.FirstAsync();
        var marker = Guid.NewGuid().ToString("N");

        var postType = await ef.PostTypes.Include(s => s.MetaFields).FirstAsync(s => s.TypeName == "post");

        var stringKey = $"sub_{marker}"[..12];
        var selectKey = $"state_{marker}"[..12];
        var relationKey = $"rel_{marker}"[..12];
        var variantId = Guid.NewGuid();

        var stringField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Subtitle",
            Key = stringKey,
            Type = EMetaFieldType.String,
            CreatedAt = DateTimeOffset.Now,
        };
        var selectField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "State",
            Key = selectKey,
            Type = EMetaFieldType.Select,
            CreatedAt = DateTimeOffset.Now,
            Variants = [new MetaFieldVariant { Id = variantId, Key = "published", Title = "Опубликовано" }],
        };
        var relationField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Related",
            Key = relationKey,
            Type = EMetaFieldType.Relation,
            ModelName = "Post.post",
            CreatedAt = DateTimeOffset.Now,
        };

        postType.MetaFields = [.. postType.MetaFields, stringField, selectField, relationField];
        await ef.MetaFields.AddRangeAsync(stringField, selectField, relationField);

        var targetPost = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = $"target-{marker}",
            Slug = $"target-{marker}"[..20],
            PostTypeId = postType.Id,
            UserId = user.Id,
            CreatedAt = DateTimeOffset.Now,
        };
        var sourcePost = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = $"source-{marker}",
            Slug = $"source-{marker}"[..20],
            PostTypeId = postType.Id,
            UserId = user.Id,
            CreatedAt = DateTimeOffset.Now,
        };

        await ef.Posts.AddRangeAsync(targetPost, sourcePost);
        await ef.PostMetaValues.AddRangeAsync(
            new PostMetaValueEntity
            {
                Id = Guid.NewGuid(),
                PostId = sourcePost.Id,
                MetaFieldId = stringField.Id,
                Type = EMetaFieldType.String,
                StringShort = "sub-1",
                CreatedAt = DateTimeOffset.Now,
            },
            new PostMetaValueEntity
            {
                Id = Guid.NewGuid(),
                PostId = sourcePost.Id,
                MetaFieldId = selectField.Id,
                Type = EMetaFieldType.Select,
                VariantId = variantId,
                CreatedAt = DateTimeOffset.Now,
            },
            new PostMetaValueEntity
            {
                Id = Guid.NewGuid(),
                PostId = sourcePost.Id,
                MetaFieldId = relationField.Id,
                Type = EMetaFieldType.Relation,
                ModelId = targetPost.Id,
                CreatedAt = DateTimeOffset.Now,
            });
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var request = new ListPostQueryRequest
        {
            Search = marker,
            MetaFields = [stringKey, selectKey, relationKey],
        };

        //Act
        var result = await client.Request(_apiUrl, "by-type/post/list/offset")
                                 .AppendQueryParam(request)
                                 .GetJsonAsync<ListDataResult<PostListItemResponse>>();

        //Assert
        var item = result.Items.Should().ContainSingle(s => s.Id == sourcePost.Id).Subject;
        item.MetaColumns.Should().NotBeNull();
        item.MetaColumns![stringKey].Should().Be("sub-1");
        item.MetaColumns[selectKey].Should().Be("Опубликовано");
        item.MetaColumns[relationKey].Should().Be(targetPost.Title);

        // у поста без значений колонки пустые
        var target = result.Items.Should().ContainSingle(s => s.Id == targetPost.Id).Subject;
        target.MetaColumns![stringKey].Should().BeNull();
    }
}
