using System.Text.Json;
using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using NSubstitute;

namespace Test.Mars.Host.Services.PostJsonServices;

public class PostJsonServiceTestBase
{
    protected readonly IPostRepository _postRepository;
    protected readonly IUserRepository _userRepository;
    protected readonly IFileRepository _fileRepository;
    protected readonly IMetaFieldMaterializerService _metaFieldMaterializerService;
    protected readonly FileHostingInfo _hostingInfo;
    protected readonly IPostService _postService;
    protected readonly IMetaModelTypesLocator _metaModelTypesLocator;
    internal readonly PostJsonService _postJsonService;
    protected static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    protected readonly IFixture _fixture = new Fixture();

    public PostJsonServiceTestBase()
    {
        EntitiesCustomize.PostTypeDict = new Dictionary<string, PostTypeEntity> { ["post"] = new PostTypeEntity() };
        EntitiesCustomize.PostCategoryTypeDict = new Dictionary<string, PostCategoryTypeEntity> { ["default"] = new PostCategoryTypeEntity() };

        _fixture.Customize(new FixtureCustomize());

        _postRepository = Substitute.For<IPostRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _fileRepository = Substitute.For<IFileRepository>();
        _metaFieldMaterializerService = Substitute.For<IMetaFieldMaterializerService>();
        _hostingInfo = FileFixtureCustomizeExtension.FileHostingInfo;
        IValidatorFabric validatorFabric = Substitute.For<IValidatorFabric>();
        var postTransformer = Substitute.For<IPostTransformer>();
        postTransformer.Transform(Arg.Any<PostDetail>(), default).Returns(callInfo => callInfo.Arg<PostDetail>());

        _postService = Substitute.For<IPostService>();
        _metaModelTypesLocator = Substitute.For<IMetaModelTypesLocator>();

        _postJsonService = new PostJsonService(_postRepository,
                                            validatorFabric,
                                            _metaFieldMaterializerService,
                                            _postService,
                                            _metaModelTypesLocator,
                                            postTransformer);
    }

    protected void SetupSamplePost(Action<PostEntity> metaSetupActon)
    {
        var postTypeEntity = _fixture.Create<PostTypeEntity>();

        var postEntity = _fixture.Create<PostEntity>();
        postEntity.PostType = postTypeEntity;
        postEntity.PostTypeId = postTypeEntity.Id;
        postEntity.User = UserConstants.TestUserEnt;
        postEntity.Categories = [];

        metaSetupActon(postEntity);

        var postDetail = postEntity.ToDetail();

        _postRepository
            .GetDetail(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(postDetail);
    }

    protected MetaFieldRelatedFillDict CreateFillContext((MetaFieldType type, string? modelName, Guid id, object modelDto)[] values)
    {
        var dict = new MetaFieldRelatedFillDict(values.Length);

        foreach (var value in values)
        {
            dict.Add((value.type, value.modelName, value.id), new MetaFieldRelatedFillDictValue
            {
                ModelId = value.id,
                ModelName = value.modelName,
                Type = value.type,
                ModelDto = value.modelDto,
            });
        }

        return dict;
    }

    protected void SetupPostMetaFields_Primitives(PostEntity postEntity)
    {
        var meta1 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Bool 1",
            Key = "bool1",
            Type = EMetaFieldType.Bool,
        };
        var metaValue1 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.Bool,
            Bool = true,
            MetaField = meta1,
            MetaFieldId = meta1.Id,
        };

        var meta2 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "string 1",
            Key = "str1",
            Type = EMetaFieldType.String,
        };
        var metaValue2 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.String,
            StringShort = "string 123 ok",
            MetaField = meta2,
            MetaFieldId = meta2.Id,
        };

        var meta3 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Int 1",
            Key = "int1",
            Type = EMetaFieldType.Int,
        };
        var metaValue3 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.Int,
            Int = 123,
            MetaField = meta3,
            MetaFieldId = meta3.Id,
        };

        postEntity.MetaValues = [metaValue1, metaValue2, metaValue3];
    }

    protected void SetupPostMetaFields_Variants(PostEntity postEntity)
    {
        var meta1 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "selone 1",
            Key = "sel1",
            Type = EMetaFieldType.Select,
            Variants = [
                new(){
                    Id = Guid.NewGuid(),
                    Title = "Variant 1",
                    Value = 1
                },
                new(){
                    Id = Guid.NewGuid(),
                    Title = "Variant 2",
                    Value = 2
                },
            ]
        };
        var metaValue1 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.Select,
            VariantId = meta1.Variants[0].Id,
            MetaField = meta1,
            MetaFieldId = meta1.Id,
        };

        var meta2 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "sel Many 1",
            Key = "selmany1",
            Type = EMetaFieldType.SelectMany,
            Variants = [
                new(){
                    Id = Guid.NewGuid(),
                    Title = "ManyVariant 1",
                    Value = 1
                },
                new(){
                    Id = Guid.NewGuid(),
                    Title = "ManyVariant 2",
                    Value = 2
                },
                new(){
                    Id = Guid.NewGuid(),
                    Title = "ManyVariant 3",
                    Value = 3
                },
                new(){
                    Id = Guid.NewGuid(),
                    Title = "ManyVariant 4",
                    Value = 4
                },
            ]
        };
        var metaValue2 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.Select,
            VariantsIds = [meta2.Variants[1].Id, meta2.Variants[2].Id],
            MetaField = meta2,
            MetaFieldId = meta2.Id,
        };

        postEntity.MetaValues = [metaValue1, metaValue2];
    }

    protected void SetupPostMetaFields_FileAndImage(PostEntity postEntity, FileEntity textFile1, FileEntity imageFile1)
    {
        var meta1 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "File 1",
            Key = "file1",
            Type = EMetaFieldType.File,
        };
        var metaValue1 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.File,
            ModelId = textFile1.Id,
            MetaField = meta1,
            MetaFieldId = meta1.Id,
        };

        var meta2 = new MetaFieldEntity
        {
            Id = Guid.NewGuid(),
            Title = "Image png 1",
            Key = "image1",
            Type = EMetaFieldType.Image,
        };
        var metaValue2 = new MetaValueEntity()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Type = EMetaFieldType.Image,
            ModelId = imageFile1.Id,
            MetaField = meta2,
            MetaFieldId = meta2.Id,
        };

        postEntity.MetaValues = [metaValue1, metaValue2];
    }

    protected void SetupPostMetaFields_RelationModels(PostEntity postEntity, (string key, string modelName, Guid modelId)[] values)
    {
        postEntity.MetaValues = values.Select(value =>
        {
            var meta = new MetaFieldEntity
            {
                Id = Guid.NewGuid(),
                Title = value.key,
                Key = value.key,
                Type = EMetaFieldType.Relation,
                ModelName = value.modelName,
            };
            var metaValue = new MetaValueEntity()
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                Type = EMetaFieldType.Relation,
                ModelId = value.modelId,
                MetaField = meta,
                MetaFieldId = meta.Id,
            };

            return metaValue;
        }).ToList();
    }
}
