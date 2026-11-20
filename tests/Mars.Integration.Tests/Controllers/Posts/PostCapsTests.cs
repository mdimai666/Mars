using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Host.Controllers;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>
/// T3 «роли = наборы прав»: владение постами через капы Post.Add/Update/UpdateAny/Delete/DeleteAny/Publish.
/// Роли создаются с уникальными именами и нужным набором прав (пресеты Author/Editor — T4).
/// </summary>
/// <seealso cref="PostController"/>
public sealed class PostCapsTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    static readonly string[] AuthorCaps = [RoleCaps.PostCap.Add, RoleCaps.PostCap.Update, RoleCaps.PostCap.Delete];
    static readonly string[] EditorCaps = [.. AuthorCaps, RoleCaps.PostCap.UpdateAny, RoleCaps.PostCap.DeleteAny];
    static readonly string[] PublisherCaps = [.. AuthorCaps, RoleCaps.PostCap.Publish];

    public PostCapsTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    [IntegrationFact]
    public async Task CreatePost_WithAddCap_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Create);
        var user = CreateUserWithCaps(AuthorCaps);
        var client = AppFixture.GetClientForUser(user.Id);
        var request = _fixture.Create<CreatePostRequest>() with { Status = "draft" };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(request).CatchUserActionError();
        var result = await res.GetJsonAsync<PostDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Id.Should().Be(request.Id!.Value);
    }

    [IntegrationFact]
    public async Task CreatePost_WithoutAddCap_403()
    {
        //Arrange
        var user = CreateUserWithCaps([]);
        var client = AppFixture.GetClientForUser(user.Id);
        var request = _fixture.Create<CreatePostRequest>() with { Status = "draft" };

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task CreatePost_ToPublish_WithoutPublishCap_403()
    {
        //Arrange
        var user = CreateUserWithCaps(AuthorCaps);
        var client = AppFixture.GetClientForUser(user.Id);
        var request = _fixture.Create<CreatePostRequest>() with { Status = "publish" };

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task UpdatePost_OwnPost_WithUpdateCap_Succeeds()
    {
        //Arrange
        _ = nameof(PostController.Update);
        var owner = CreateUserWithCaps(AuthorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);
        var request = CreateUpdateRequest(post) with { Status = "draft" };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Id.Should().Be(post.Id);
        result.Title.Should().Be(request.Title);
    }

    [IntegrationFact]
    public async Task UpdatePost_ForeignPost_WithoutUpdateAny_403()
    {
        //Arrange
        var owner = CreateUserWithCaps(AuthorCaps);
        var actor = CreateUserWithCaps(AuthorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);
        var request = CreateUpdateRequest(post) with { Status = "draft" };

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task UpdatePost_ForeignPost_WithUpdateAny_Succeeds()
    {
        //Arrange
        var owner = CreateUserWithCaps(AuthorCaps);
        var editor = CreateUserWithCaps(EditorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(editor.Id);
        var request = CreateUpdateRequest(post) with { Status = "draft" };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Id.Should().Be(post.Id);
        result.Title.Should().Be(request.Title);
    }

    [IntegrationFact]
    public async Task UpdatePost_ToPublish_WithoutPublishCap_403()
    {
        //Arrange — свой пост, право редактирования есть, публикации нет
        var owner = CreateUserWithCaps(AuthorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);
        var request = CreateUpdateRequest(post) with { Status = "publish" };

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task UpdatePost_ToPublish_WithPublishCap_Succeeds()
    {
        //Arrange
        var owner = CreateUserWithCaps(PublisherCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(owner.Id);
        var request = CreateUpdateRequest(post) with { Status = "publish" };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        result.Id.Should().Be(post.Id);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Include(s => s.PostStatus).First(s => s.Id == post.Id)
            .PostStatus!.Slug.Should().Be("publish");
    }

    [IntegrationFact]
    public async Task DeletePost_ForeignPost_WithoutDeleteAny_403()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var owner = CreateUserWithCaps(AuthorCaps);
        var actor = CreateUserWithCaps(AuthorCaps);
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
    public async Task DeletePost_ForeignPost_WithDeleteAny_Succeeds()
    {
        //Arrange
        var owner = CreateUserWithCaps(AuthorCaps);
        var editor = CreateUserWithCaps(EditorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(editor.Id);

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(post.Id).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Posts.Any(s => s.Id == post.Id).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task DeleteMany_MixedOwnAndForeign_WithoutDeleteAny_403AndNothingDeleted()
    {
        //Arrange
        _ = nameof(PostController.DeleteMany);
        var owner = CreateUserWithCaps(AuthorCaps);
        var actor = CreateUserWithCaps(AuthorCaps);
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
    public async Task GetEditModel_ForeignPost_WithoutUpdateAny_403()
    {
        //Arrange — эндпоинт [Authorize], проверка кап по владельцу в сервисе
        _ = nameof(PostController.GetEditModel);
        var owner = CreateUserWithCaps(AuthorCaps);
        var actor = CreateUserWithCaps(AuthorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(actor.Id);

        //Act
        var result = await client.Request(_apiUrl, "edit", post.Id).AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task GetEditModel_ForeignPost_WithUpdateAny_Succeeds()
    {
        //Arrange
        var owner = CreateUserWithCaps(AuthorCaps);
        var editor = CreateUserWithCaps(EditorCaps);
        var post = CreatePost(owner.Id);
        var client = AppFixture.GetClientForUser(editor.Id);

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
        };

    PostEntity CreatePost(Guid ownerId)
    {
        var ef = AppFixture.MarsDbContext();
        var post = _fixture.Create<PostEntity>();
        post.UserId = ownerId;
        ef.Posts.Add(post);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        return post;
    }

    UserEntity CreateUserWithCaps(string[] caps)
    {
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "PostCapsRole-" + suffix,
            NormalizedName = ("PostCapsRole-" + suffix).ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };
        ef.Roles.Add(role);
        ef.RoleClaims.AddRange(caps.Select(c => new RoleClaimEntity
        {
            RoleId = role.Id,
            ClaimType = RoleCaps.ClaimType,
            ClaimValue = c,
        }));

        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var createdUser = _fixture.Create<UserEntity>();
        createdUser.UserName = "capsuser-" + suffix;
        createdUser.NormalizedUserName = createdUser.UserName.ToUpper();
        createdUser.Email = "capsuser-" + suffix + "@mail.localhost";
        createdUser.NormalizedEmail = createdUser.Email.ToUpper();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = userType.Id;
        ef.Users.Add(createdUser);
        ef.UserRoles.Add(new UserRoleEntity { UserId = createdUser.Id, RoleId = role.Id });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        Cache.Invalidate();
        return createdUser;
    }
}
