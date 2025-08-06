using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Test.Mars.Host.Services;

public class MetaFieldMaterializerServiceTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly FileHostingInfo _hostingInfo;
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly MetaFieldMaterializerService _metaFieldMaterializerService;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileRepository _fileRepository;

    public MetaFieldMaterializerServiceTests()
    {
        EntitiesCustomize.PostTypeDict = new() { ["post"] = new PostTypeEntity() };
        EntitiesCustomize.UserTypeDict = new() { [UserTypeEntity.DefaultTypeName] = UserConstants.TestUserType };

        _fixture.Customize(new FixtureCustomize());

        _metaModelTypesLocator = Substitute.For<IMetaModelTypesLocator>();
        _hostingInfo = FileFixtureCustomizeExtension.FileHostingInfo;
        _serviceProvider = Substitute.For<IKeyedServiceProvider>();
        _postRepository = Substitute.For<IPostRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _fileRepository = Substitute.For<IFileRepository>();

        _serviceProvider.GetService(typeof(IPostRepository)).Returns(_postRepository);
        _serviceProvider.GetService(typeof(IUserRepository)).Returns(_userRepository);
        _serviceProvider.GetService(typeof(IFileRepository)).Returns(_fileRepository);

        _metaFieldMaterializerService = new MetaFieldMaterializerService(_metaModelTypesLocator, _serviceProvider, Options.Create(_hostingInfo));

    }

    (UserDetail user1, PostDetail post1) SetupData()
    {
        var postTypeEntity = _fixture.Create<PostTypeEntity>();
        var author = UserConstants.TestUserEnt.ToPostAuthor();
        var user1 = UserConstants.TestUserEnt.ToDetail();
        var post1 = _fixture.Build<PostDetail>()
                            .Without(s => s.MetaValues)
                            .With(s => s.Type, postTypeEntity.TypeName)
                            .With(s => s.Author, author)
                            .Create();

        _userRepository.ListAllDetail(Arg.Any<ListAllUserQuery>(), Arg.Any<CancellationToken>())
            .Returns([user1]);
        _postRepository.ListAllDetail(Arg.Any<ListAllPostQuery>(), Arg.Any<CancellationToken>())
            .Returns([post1]);

        // User
        var userListHandler = Substitute.For<IMetaRelationModelProviderHandler>();
        userListHandler.ListHandle(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, object>()
            {
                [user1.Id] = user1
            });

        _serviceProvider.GetKeyedService(typeof(IMetaRelationModelProviderHandler), "User")
            .Returns(userListHandler);

        // Post
        var postListHandler = Substitute.For<IMetaRelationModelProviderHandler>();
        postListHandler.ListHandle(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, object>()
            {
                [post1.Id] = post1
            });

        _serviceProvider.GetKeyedService(typeof(IMetaRelationModelProviderHandler), "Post")
            .Returns(postListHandler);

        return (user1, post1);
    }

    [Fact]
    public async Task GetModelByIds_ValidRequest_ReturnObjects()
    {
        //Arrange
        _ = nameof(MetaFieldMaterializerService.GetModelByIds);
        var (user1, _) = SetupData();

        var query = new MetaFieldMaterializerQuery { Ids = [], ModelName = "User", Type = MetaFieldType.Relation };

        //Act
        var result = await _metaFieldMaterializerService.GetModelByIds(query, default);

        //Assert
        result.Count.Should().Be(1);
        result[user1.Id].ModelDto.Should().Be(user1);
    }

    [Fact]
    public async Task GetFillContext_RelationValueFill_ReturnFullDictionary()
    {
        //Arrange
        _ = nameof(MetaFieldMaterializerService.GetFillContext);
        var (user1, post1) = SetupData();

        //Act
        var result = await _metaFieldMaterializerService.GetFillContext([
            GetMeta(MetaFieldType.Relation, "user1", "User", user1.Id, user1),
            GetMeta(MetaFieldType.Relation, "post1", "Post.post", post1.Id, post1),
            ], default);

        //Assert
        var u1 = result[(MetaFieldType.Relation, "User", user1.Id)];
        u1.ModelDto.Should().Be(user1);
        u1.ModelId.Should().Be(user1.Id);
        u1.Type.Should().Be(MetaFieldType.Relation);
        u1.ModelName.Should().Be("User");

        result[(MetaFieldType.Relation, "Post.post", post1.Id)].ModelDto.Should().Be(post1);
    }

    private static MetaValueDto GetMeta(MetaFieldType type, string key, string modelName, Guid modelId, object modelDto)
    {
        var meta = new MetaFieldDto
        {
            Id = Guid.NewGuid(),
            Key = key,
            ModelName = modelName,
            Type = type,
            ParentId = Guid.Empty,

            MaxValue = default,
            MinValue = default,
            Order = 0,
            Tags = [],
            Variants = [],
            Title = key,
            Disabled = false,
            Hidden = false,
            IsNullable = false,
            Description = "",
        };
        return new MetaValueDto
        {
            Id = Guid.NewGuid(),
            MetaField = meta,
            Type = type,
            ModelId = modelId,
            Value = modelDto,
            VariantId = null,
            VariantsIds = null,
            Index = 0
        };
    }
}
