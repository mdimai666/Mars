using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>
/// «Роли = наборы прав»: Update/Delete/DeleteMany/GetEditModel — владелец с правами Post.Update/Delete
/// или Admin. Не-админ — пользователь с ролью Author (пресет T4: Post.Add/Update/Delete + Media.Add,
/// без Any-прав); админ-клиент фикстуры — TestUser (роль Admin). Семантика владения сохранена.
/// </summary>
/// <seealso cref="PostController"/>
public sealed class PostOwnershipTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public PostOwnershipTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    [IntegrationFact]
    public async Task UpdatePost_ForeignPostNonAdmin_403Forbidden()
    {
        //Arrange
        _ = nameof(PostController.Update);
        var owner = CreateUserWithRole("Author");
        var actor = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);

        var request = CreateUpdateRequest(post);

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.First(s => s.Id == post.Id).Title.Should().Be(post.Title);
    }

    [IntegrationFact]
    public async Task UpdatePost_OwnPostNonAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Update);
        var owner = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);

        var request = CreateUpdateRequest(post);

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Id.Should().Be(post.Id);
        result.Title.Should().Be(request.Title);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.First(s => s.Id == post.Id).Title.Should().Be(request.Title);
    }

    [IntegrationFact]
    public async Task UpdatePost_ForeignPostAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Update);
        var owner = CreateUserWithRole("Viewer");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClient(); // TestUser — Admin

        var request = CreateUpdateRequest(post);

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Id.Should().Be(post.Id);
        result.Title.Should().Be(request.Title);
    }

    [IntegrationFact]
    public async Task DeletePost_ForeignPostNonAdmin_403Forbidden()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var owner = CreateUserWithRole("Author");
        var actor = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(post.Id).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => s.Id == post.Id).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task DeletePost_OwnPostNonAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var owner = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(post.Id).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => s.Id == post.Id).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeletePost_ForeignPostAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var owner = CreateUserWithRole("Viewer");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClient(); // TestUser — Admin

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(post.Id).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => s.Id == post.Id).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeleteMany_MixedOwnAndForeignNonAdmin_403ForbiddenAndNothingDeleted()
    {
        //Arrange
        _ = nameof(PostController.DeleteMany);
        var owner = CreateUserWithRole("Author");
        var actor = CreateUserWithRole("Author");
        var ownPost = CreatePost(actor.Id);
        var foreignPost = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);

        var ids = new[] { ownPost.Id, foreignPost.Id };

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids }).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Count(s => ids.Contains(s.Id)).Should().Be(2);
    }

    [IntegrationFact]
    public async Task DeleteMany_OwnPostsNonAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.DeleteMany);
        var owner = CreateUserWithRole("Author");
        var posts = new[] { CreatePost(owner.Id), CreatePost(owner.Id) };
        var client = AppFixture.GetClientForUser(owner.Id);

        var ids = posts.Select(s => s.Id).ToArray();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids }).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => ids.Contains(s.Id)).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task GetEditModel_ForeignPostNonAdmin_403Forbidden()
    {
        //Arrange
        _ = nameof(PostController.GetEditModel);
        var owner = CreateUserWithRole("Author");
        var actor = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);

        //Act
        var result = await client.Request(_apiUrl, "edit", post.Id).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task GetEditModel_OwnPostNonAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.GetEditModel);
        var owner = CreateUserWithRole("Author");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);

        //Act
        var result = await client.Request(_apiUrl, "edit", post.Id).GetAsync().CatchUserActionError().ReceiveJson<PostEditViewModel>();

        //Assert
        result.Post.Id.Should().Be(post.Id);
    }

    [IntegrationFact]
    public async Task GetEditModel_ForeignPostAdmin_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.GetEditModel);
        var owner = CreateUserWithRole("Viewer");
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClient(); // TestUser — Admin

        //Act
        var result = await client.Request(_apiUrl, "edit", post.Id).GetAsync().CatchUserActionError().ReceiveJson<PostEditViewModel>();

        //Assert
        result.Post.Id.Should().Be(post.Id);
    }

    UpdatePostRequest CreateUpdateRequest(PostEntity post)
        => _fixture.Create<UpdatePostRequest>() with
        {
            Id = post.Id,
            Type = "post",
            Slug = post.Slug,
            // фикстурный Status случаен (в т.ч. publish); тест про владение, не про публикацию —
            // фиксируем draft, чтобы не триггерить отдельное право Post.Publish
            Status = "draft",
        };

    PostEntity CreatePost(Guid ownerId)
    {
        var ef = AppFixture.MarsDbContext();
        var post = _fixture.Create<PostEntity>();
        post.UserId = ownerId;
        ef.Posts.Add(post);
        ef.SaveChanges();
        return post;
    }

    UserEntity CreateUserWithRole(string roleName)
    {
        // кэш прав — singleton фикстуры: перечитываем свежий сид пресетов (Author)
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();

        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var role = ef.Roles.AsNoTracking().First(s => s.Name == roleName);
        var createdUser = _fixture.Create<UserEntity>();
        // уникальные UserName/Email — в тесте может быть несколько пользователей
        var suffix = Guid.NewGuid().ToString("N")[..8];
        createdUser.UserName = "user-" + suffix;
        createdUser.NormalizedUserName = createdUser.UserName.ToUpper();
        createdUser.Email = "user-" + suffix + "@mail.localhost";
        createdUser.NormalizedEmail = createdUser.Email.ToUpper();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = userType.Id;
        ef.Users.Add(createdUser);
        ef.UserRoles.Add(new UserRoleEntity { UserId = createdUser.Id, RoleId = role.Id });
        ef.SaveChanges();
        return createdUser;
    }
}
