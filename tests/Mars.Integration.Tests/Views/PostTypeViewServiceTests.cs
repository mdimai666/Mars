using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Views;

/// <seealso cref="IPostTypeViewService"/>
public class PostTypeViewServiceTests : ApplicationTests
{
    public PostTypeViewServiceTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    public class ViewRow
    {
        public Guid Id { get; set; }
        public string? Slug { get; set; }
        public string? Title { get; set; }
        public string? subtitle { get; set; }
        public int? views_count { get; set; }
    }

    [IntegrationFact]
    public async Task ListFromView_ReturnsMetaColumns_AndSupportsPruningAndRegeneration()
    {
        //Arrange
        var ef = AppFixture.MarsDbContext();
        var user = await ef.Users.FirstAsync();

        var typeName = $"viewtest{Guid.NewGuid():N}"[..16];
        var stringField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Subtitle",
            Key = "subtitle",
            Type = EMetaFieldType.String,
            CreatedAt = DateTimeOffset.Now,
        };
        var intField = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Views",
            Key = "views_count",
            Type = EMetaFieldType.Int,
            CreatedAt = DateTimeOffset.Now,
        };
        var postType = new PostTypeEntity
        {
            Id = Guid.NewGuid(),
            Title = "View test",
            TypeName = typeName,
            CreatedAt = DateTimeOffset.Now,
            MetaFields = [stringField, intField],
        };
        var post = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Hello view",
            Slug = $"hello-view-{Guid.NewGuid():N}"[..20],
            PostTypeId = postType.Id,
            UserId = user.Id,
            CreatedAt = DateTimeOffset.Now,
        };

        ef.PostTypes.Add(postType);
        ef.Posts.Add(post);
        ef.PostMetaValues.AddRange(
            new PostMetaValueEntity
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                MetaFieldId = stringField.Id,
                Type = EMetaFieldType.String,
                StringShort = "sub-1",
                CreatedAt = DateTimeOffset.Now,
            },
            new PostMetaValueEntity
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                MetaFieldId = intField.Id,
                Type = EMetaFieldType.Int,
                Int = 42,
                CreatedAt = DateTimeOffset.Now,
            });
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();
        var viewService = AppFixture.ServiceProvider.GetRequiredService<IPostTypeViewService>();

        //Act: полное чтение
        var rows = await viewService.ListFromViewAsync<ViewRow>(typeName);

        //Assert
        rows.Should().ContainSingle();
        rows[0].Id.Should().Be(post.Id);
        rows[0].Slug.Should().Be(post.Slug);
        rows[0].Title.Should().Be(post.Title);
        rows[0].subtitle.Should().Be("sub-1");
        rows[0].views_count.Should().Be(42);

        //Act: column pruning — только часть колонок
        var pruned = await viewService.ListFromViewAsync<ViewRow>(typeName, properties: ["Slug", "views_count"]);

        //Assert
        pruned.Should().ContainSingle();
        pruned[0].Slug.Should().Be(post.Slug);
        pruned[0].views_count.Should().Be(42);
        pruned[0].subtitle.Should().BeNull(); // не запрошена — не заполнена

        //Act: представление удаляется и по требованию генерируется заново
        await viewService.DropViewAsync(typeName);
        var regenerated = await viewService.ListFromViewAsync<ViewRow>(typeName);

        //Assert
        regenerated.Should().ContainSingle();
        regenerated[0].subtitle.Should().Be("sub-1");
    }
}
