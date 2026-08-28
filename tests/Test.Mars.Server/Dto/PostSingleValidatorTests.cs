using FluentAssertions;
using Mars.Cms.Abstractions.Dto.PostCategories;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostTypes;
using NSubstitute;

namespace Test.Mars.Server.Dto;

/// <summary>
/// Правила single-типа (фича <see cref="PostTypeConstants.Features.Single"/>):
/// запрет второго поста, запрет удаления, запрет включения фичи типу с 2+ постами.
/// </summary>
public class PostSingleValidatorTests
{
    static PostTypeDetail PostType(Guid id, IReadOnlyCollection<string> features)
        => new()
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Title = "Настройки",
            TypeName = "settings",
            Tags = [],
            EnabledFeatures = features,
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
            ModifiedAt = null,
            PostStatusList = [],
            MetaFields = [],
            Presentation = PostTypePresentation.Default(),
        };

    static CreatePostQuery CreateQuery(string type)
        => new()
        {
            Title = "t",
            Type = type,
            Slug = "valid-slug",
            Tags = [],
            UserId = Guid.NewGuid(),
            Status = null,
            Content = null,
            Excerpt = null,
            LangCode = "",
            CategoryIds = [],
            MetaValues = [],
        };

    static PostSummary Post(string type)
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = null,
            Title = "t",
            Type = type,
            Slug = "s",
            Tags = [],
            Author = new PostAuthor { Id = Guid.Empty, UserName = "", DisplayName = "" },
            Status = null,
            Categories = null,
        };

    static UpdatePostTypeQuery UpdateQuery(PostTypeDetail postType, IReadOnlyCollection<string> features)
        => new()
        {
            Id = postType.Id,
            Title = postType.Title,
            TypeName = postType.TypeName,
            Tags = [],
            PostStatusList = [],
            EnabledFeatures = features,
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
            MetaFields = [],
            ImageFieldKey = null,
        };

    static IMetaModelTypesLocator Locator(PostTypeDetail postType)
    {
        var locator = Substitute.For<IMetaModelTypesLocator>();
        locator.GetPostTypeByName(postType.TypeName).Returns(postType);
        return locator;
    }

    [Fact]
    public async Task CreatePost_SingleTypeAlreadyHasPost_Fails()
    {
        var postType = PostType(Guid.NewGuid(), [PostTypeConstants.Features.Single]);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.CountByTypeAsync(postType.Id, Arg.Any<CancellationToken>()).Returns(1);

        var validator = new CreatePostQueryValidator(Locator(postType),
                                                     Substitute.For<IPostCategoryRepository>(),
                                                     Substitute.For<IMetaValuesValidator>(),
                                                     postRepository);

        var result = await validator.ValidateAsync(CreateQuery(postType.TypeName));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePostQuery.Type)
                                            && e.ErrorMessage.Contains("единственную запись"));
    }

    [Fact]
    public async Task CreatePost_SingleTypeWithoutPosts_Passes()
    {
        var postType = PostType(Guid.NewGuid(), [PostTypeConstants.Features.Single]);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.CountByTypeAsync(postType.Id, Arg.Any<CancellationToken>()).Returns(0);

        var validator = new CreatePostQueryValidator(Locator(postType),
                                                     Substitute.For<IPostCategoryRepository>(),
                                                     Substitute.For<IMetaValuesValidator>(),
                                                     postRepository);

        var result = await validator.ValidateAsync(CreateQuery(postType.TypeName));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePost_RegularTypeWithPosts_Passes()
    {
        var postType = PostType(Guid.NewGuid(), []);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.CountByTypeAsync(postType.Id, Arg.Any<CancellationToken>()).Returns(5);

        var validator = new CreatePostQueryValidator(Locator(postType),
                                                     Substitute.For<IPostCategoryRepository>(),
                                                     Substitute.For<IMetaValuesValidator>(),
                                                     postRepository);

        var result = await validator.ValidateAsync(CreateQuery(postType.TypeName));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePost_SingleType_Fails()
    {
        var postType = PostType(Guid.NewGuid(), [PostTypeConstants.Features.Single]);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.Get(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Post(postType.TypeName));

        var validator = new DeletePostQueryValidator(postRepository, Locator(postType));

        var result = await validator.ValidateAsync(Guid.NewGuid());

        result.Errors.Should().Contain(e => e.PropertyName == "Id"
                                            && e.ErrorMessage.Contains("удаление запрещено"));
    }

    [Fact]
    public async Task DeletePost_RegularType_Passes()
    {
        var postType = PostType(Guid.NewGuid(), []);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.Get(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Post(postType.TypeName));

        var validator = new DeletePostQueryValidator(postRepository, Locator(postType));

        var result = await validator.ValidateAsync(Guid.NewGuid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EnableSingle_TypeWithTwoPosts_Fails()
    {
        var postType = PostType(Guid.NewGuid(), []);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.CountByTypeAsync(postType.Id, Arg.Any<CancellationToken>()).Returns(2);

        var validator = new UpdatePostTypeQueryValidator(Locator(postType), postRepository);

        var result = await validator.ValidateAsync(UpdateQuery(postType, [PostTypeConstants.Features.Single]));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePostTypeQuery.EnabledFeatures)
                                            && e.ErrorMessage.Contains("Единственная запись"));
    }

    [Fact]
    public async Task EnableSingle_TypeWithOnePost_Passes()
    {
        var postType = PostType(Guid.NewGuid(), []);
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.CountByTypeAsync(postType.Id, Arg.Any<CancellationToken>()).Returns(1);

        var validator = new UpdatePostTypeQueryValidator(Locator(postType), postRepository);

        var result = await validator.ValidateAsync(UpdateQuery(postType, [PostTypeConstants.Features.Single]));

        result.IsValid.Should().BeTrue();
    }
}
