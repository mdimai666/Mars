using AutoFixture;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using Mars.Data.Entities;
using Mars.Forms.Abstractions;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// T3 «роли = наборы прав»: фоновый контекст (IRequestContext.User is null — флоу/scheduler)
/// не ограничивается кап-проверками PostService. Пробел фазы 0: не было теста на null user.
/// </summary>
public sealed class PostServiceBackgroundContextTests : ApplicationTests
{
    public PostServiceBackgroundContextTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    PostService CreateBackgroundService(IServiceScope scope)
    {
        // User/Roles у substitute null — как в фоновом флоу без HTTP-пользователя
        var requestContext = Substitute.For<IRequestContext>();

        return new PostService(
            scope.ServiceProvider.GetRequiredService<IPostRepository>(),
            scope.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>(),
            scope.ServiceProvider.GetRequiredService<IEventManager>(),
            requestContext,
            scope.ServiceProvider.GetRequiredService<IValidatorFactory>(),
            scope.ServiceProvider.GetRequiredService<IPostTransformer>(),
            scope.ServiceProvider.GetRequiredService<IMetaValuesGeneratorService>(),
            scope.ServiceProvider.GetRequiredService<IFormDefinitionNormalizer>(),
            AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>());
    }

    [IntegrationFact]
    public async Task Delete_BackgroundNullUser_Succeeds()
    {
        //Arrange
        var post = CreatePost();
        using var scope = AppFixture.ServiceProvider.CreateScope();
        var service = CreateBackgroundService(scope);

        //Act
        var deleted = await service.Delete(post.Id, default);

        //Assert
        deleted.Id.Should().Be(post.Id);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => s.Id == post.Id).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeleteMany_BackgroundNullUser_Succeeds()
    {
        //Arrange
        var posts = new[] { CreatePost(), CreatePost() };
        using var scope = AppFixture.ServiceProvider.CreateScope();
        var service = CreateBackgroundService(scope);

        //Act
        var deleted = await service.DeleteMany(new DeleteManyPostQuery { Ids = posts.Select(s => s.Id).ToArray() }, default);

        //Assert
        deleted.Should().HaveCount(2);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        var ids = posts.Select(s => s.Id).ToArray();
        ef.Posts.Any(s => ids.Contains(s.Id)).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Update_BackgroundNullUser_Succeeds()
    {
        //Arrange
        var post = CreatePost();
        using var scope = AppFixture.ServiceProvider.CreateScope();
        var service = CreateBackgroundService(scope);

        var query = new UpdatePostQuery
        {
            Id = post.Id,
            Title = "Updated by background flow",
            Type = "post",
            Slug = post.Slug,
            Tags = [],
            UserId = post.UserId,
            Status = "draft",
            Content = post.Content,
            Excerpt = null,
            LangCode = post.LangCode,
            CategoryIds = [],
            MetaValues = null,
        };

        //Act
        var updated = await service.Update(query, default);

        //Assert
        updated.Title.Should().Be(query.Title);
    }

    PostEntity CreatePost()
    {
        var ef = AppFixture.MarsDbContext();
        var post = _fixture.Create<PostEntity>();
        ef.Posts.Add(post);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        return post;
    }
}
