using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Repositories.Helpers;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Mappings.PostJsons;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using NSubstitute;

namespace Test.Mars.Host.Services.PostJsonServices;

public sealed class GetPostJsonServiceTests : PostJsonServiceTestBase
{
    [Fact]
    public async Task GetDetailFromRepository_RetriveMetaFields_PrimitivesValuesSuccess()
    {
        //Arrange
        _ = nameof(PostRepository.GetDetail);
        SetupSamplePost(SetupPostMetaFields_Primitives);

        //Act
        var postDetail = await _postRepository.GetDetail(Guid.NewGuid(), default);

        //Assert
        postDetail.Should().NotBeNull();
        var json = JsonSerializer.Serialize(postDetail, _jsonSerializerOptions);
        postDetail.MetaValues.Should().HaveCount(3);

    }

    [Fact]
    public async Task GetDetail_PrimitiveMetaFieldsAsLikeJson_JsonSuccess()
    {
        //Arrange
        _ = nameof(PostJsonService.GetDetail);
        _ = nameof(PostJsonMapping.ConvertObjectValue);
        SetupSamplePost(SetupPostMetaFields_Primitives);

        //Act
        var postJsonNode = await _postJsonService.GetDetail(Guid.NewGuid(), default);

        //Assert
        postJsonNode.Should().NotBeNull();
        var json = JsonSerializer.Serialize(postJsonNode, _jsonSerializerOptions);
        postJsonNode.Meta.Should().HaveCount(3);
        var meta = JsonSerializer.SerializeToNode(postJsonNode.Meta, _jsonSerializerOptions);
        meta["bool1"].GetValue<bool>().Should().BeTrue();
        meta["int1"].GetValue<int>().Should().Be(123);
        meta["str1"].GetValue<string>().Should().Be("string 123 ok");

    }

    [Fact]
    public async Task GetDetail_VariantMetaFieldsAsLikeJson_JsonSuccess()
    {
        //Arrange
        _ = nameof(PostJsonService.GetDetail);
        _ = nameof(PostJsonMapping.ConvertObjectValue);
        SetupSamplePost(SetupPostMetaFields_Variants);
        var postDetail = await _postRepository.GetDetail(Guid.NewGuid(), default);

        //Act
        var postJsonNode = await _postJsonService.GetDetail(Guid.NewGuid(), default);

        //Assert
        postJsonNode.Should().NotBeNull();
        var json = JsonSerializer.Serialize(postJsonNode, _jsonSerializerOptions);
        postJsonNode.Meta.Should().HaveCount(2);
        var meta = JsonSerializer.SerializeToNode(postJsonNode.Meta, _jsonSerializerOptions);

        //1. sel
        var meta_sel1 = meta["sel1"].Deserialize<MetaFieldVariantValueDto>();
        var meta1_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "sel1");
        meta_sel1.Should().NotBeNull();
        meta_sel1.Id.Should().Be(meta1_pure.MetaField.Variants!.ElementAt(0).Id);
        meta_sel1.Title.Should().Be((meta1_pure.Value as MetaFieldVariantDto).Title);

        //1. many
        var meta_selmany1 = meta["selmany1"].Deserialize<MetaFieldVariantValueDto[]>();
        var meta2_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "selmany1");
        meta_selmany1.Should().NotBeNull();
        var variants_selmany1 = meta2_pure.MetaField.Variants!.ToArray();
        meta_selmany1!.Select(s => s.Id).Should().BeEquivalentTo([variants_selmany1[1].Id, variants_selmany1[2].Id]);
        for (int i = 0; i < meta_selmany1.Length; i++)
        {
            meta_selmany1[i].Title.Should().Be(variants_selmany1[i + 1].Title);
        }
    }

    [Fact]
    public async Task GetDetail_FileAndImageMetaFieldsAsLikeJson_JsonSuccess()
    {
        //Arrange
        _ = nameof(PostJsonService.GetDetail);
        _ = nameof(MetaValueMapping.ConvertObjectValue);
        _ = nameof(PostJsonMapping.ConvertObjectValue);
        var textFile1 = _fixture.Create<FileEntity>();
        var imageFile1 = _fixture.CreateImagePng();
        SetupSamplePost(post => SetupPostMetaFields_FileAndImage(post, textFile1, imageFile1));
        var postDetail = await _postRepository.GetDetail(Guid.NewGuid(), default);
        var resolver = new ImagePreviewResolver(new(), _hostingInfo);

        _metaFieldMaterializerService.GetFillContext(Arg.Any<IEnumerable<MetaValueDto>>(), Arg.Any<CancellationToken>())
                .Returns(CreateFillContext([
                    (MetaFieldType.File, null, textFile1.Id,  textFile1.ToDetail(resolver)),
                    (MetaFieldType.Image, null, imageFile1.Id,  imageFile1.ToDetail(resolver)),
                ]));

        //Act
        var postJsonNode = await _postJsonService.GetDetail(Guid.NewGuid(), default);

        //Assert
        postJsonNode.Should().NotBeNull();
        var json = JsonSerializer.Serialize(postJsonNode, _jsonSerializerOptions);
        postJsonNode.Meta.Should().HaveCount(2);
        var meta = JsonSerializer.SerializeToNode(postJsonNode.Meta, _jsonSerializerOptions);

        //1. textfile
        var meta_file1 = meta["file1"].Deserialize<FileDetail>();
        var meta1_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "file1");
        meta_file1.Should().NotBeNull();
        meta_file1.Id.Should().Be(meta1_pure.ModelId!.Value);
        meta_file1.FilePhysicalPath.Should().Be(textFile1.FilePhysicalPath);

        //2. imagefile
        var meta_image1 = meta["image1"].Deserialize<FileDetail>();
        var meta2_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "image1");
        meta_image1.Should().NotBeNull();
        meta_image1.Id.Should().Be(meta2_pure.ModelId!.Value);
        meta_image1.FilePhysicalPath.Should().Be(imageFile1.FilePhysicalPath);
        meta_image1.Meta.Should().NotBeNull();
        meta_image1.Meta.Thumbnails.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public async Task GetDetail_RelatedModelsMetaFieldsAsLikeJson_JsonSuccess()
    {
        //Arrange
        _ = nameof(PostJsonService.GetDetail);
        _ = nameof(MetaValueMapping.ConvertObjectValue);
        _ = nameof(PostJsonMapping.ConvertObjectValue);
        _ = nameof(MetaFieldMaterializerService.GetModelByIds);

        var postTypeEntity = _fixture.Create<PostTypeEntity>();
        var author = UserConstants.TestUserEnt.ToPostAuthor();
        var user1 = UserConstants.TestUserEnt.ToDetail();
        var post1 = _fixture.Build<PostDetail>()
                            .Without(s => s.MetaValues)
                            .With(s => s.Type, postTypeEntity.TypeName)
                            .With(s => s.Author, author)
                            .Create();

        SetupSamplePost(post => SetupPostMetaFields_RelationModels(post, [("user1", "User", user1.Id), ("post1", "Post.post", post1.Id)]));
        var postDetail = await _postRepository.GetDetail(Guid.NewGuid(), default);

        _metaFieldMaterializerService.GetFillContext(Arg.Any<IEnumerable<MetaValueDto>>(), Arg.Any<CancellationToken>())
                .Returns(CreateFillContext([
                    (MetaFieldType.Relation, "User", user1.Id,  user1),
                    (MetaFieldType.Relation, "Post.post", post1.Id,  post1),
                ]));

        //Act
        var postJsonNode = await _postJsonService.GetDetail(Guid.NewGuid(), default);

        //Assert
        postJsonNode.Should().NotBeNull();
        var json = JsonSerializer.Serialize(postJsonNode, _jsonSerializerOptions);
        postJsonNode.Meta.Should().HaveCount(2);
        var meta = JsonSerializer.SerializeToNode(postJsonNode.Meta, _jsonSerializerOptions);

        //1. User
        var meta_user1 = meta["user1"].Deserialize<UserDetail>();
        var meta1_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "user1");
        meta_user1.Should().NotBeNull();
        meta_user1.Id.Should().Be(meta1_pure.ModelId!.Value);
        meta_user1.FirstName.Should().Be(UserConstants.TestUserFirstName);

        //2. Post.post
        var meta_image1 = meta["post1"].Deserialize<PostDetail>();
        var meta2_pure = postDetail.MetaValues.First(s => s.MetaField.Key == "post1");
        meta_image1.Should().NotBeNull();
        meta_image1.Id.Should().Be(meta2_pure.ModelId!.Value);
        meta_image1.Type.Should().Be(post1.Type);
        meta_image1.Title.Should().Be(post1.Title);
    }

}
