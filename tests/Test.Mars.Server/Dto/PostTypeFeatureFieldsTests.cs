using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;

namespace Test.Mars.Server.Dto;

public class PostTypeFeatureFieldsTests
{
    static MetaFieldDto Field(MetaFieldType type, string key, JsonNode? options = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = false,
            Default = null,
            Options = options,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    [Fact]
    public void Disabled_ClearsPointer_FieldsUntouched()
    {
        var imageField = Field(MetaFieldType.Image, "image");

        var (fields, pointer) = PostTypeFeatureFields.ApplyFeaturePostImage([imageField], enable: false);

        pointer.Should().BeNull();
        fields.Should().BeEquivalentTo(new[] { imageField });
    }

    [Fact]
    public void Enabled_ValidKey_UsesField_NoCreate()
    {
        var imageField = Field(MetaFieldType.Image, "photo");

        var (fields, pointer) = PostTypeFeatureFields.ApplyFeaturePostImage([imageField], enable: true, key: "photo");

        pointer.Should().Be("photo");
        fields.Should().ContainSingle().Which.Id.Should().Be(imageField.Id);
    }

    [Fact]
    public void Enabled_NoKey_CreatesFeatureFieldWithMarker()
    {
        var (fields, pointer) = PostTypeFeatureFields.ApplyFeaturePostImage([], enable: true);

        pointer.Should().Be(FeatureFieldsCatalog.PostImageFieldKey);

        var created = fields.Should().ContainSingle().Which;
        created.Type.Should().Be(MetaFieldType.Image);
        created.Options.GetFeatureKey().Should().Be(FeatureFieldsCatalog.PostImage);
    }

    [Fact]
    public void Enabled_KeyNotFound_CreatesField()
    {
        var imageField = Field(MetaFieldType.Image, "photo");

        var (fields, pointer) = PostTypeFeatureFields.ApplyFeaturePostImage([imageField], enable: true, key: "missing");

        pointer.Should().Be(FeatureFieldsCatalog.PostImageFieldKey);
        fields.Should().HaveCount(2);
    }

    [Fact]
    public void Enabled_KeyOfNonImageField_CreatesFieldWithSuffix()
    {
        var stringField = Field(MetaFieldType.String, "image");

        var (fields, pointer) = PostTypeFeatureFields.ApplyFeaturePostImage([stringField], enable: true, key: "image");

        pointer.Should().Be($"{FeatureFieldsCatalog.PostImageFieldKey}_2");
        fields.Should().HaveCount(2);
    }

    [Fact]
    public void Content_Disabled_FieldsUntouched()
    {
        var textField = Field(MetaFieldType.Text, "body");

        var fields = PostTypeFeatureFields.ApplyFeatureContent([textField], enable: false);

        fields.Should().BeEquivalentTo(new[] { textField });
    }

    [Fact]
    public void Content_Enabled_NoField_CreatesFeatureFieldWithMarkerAndEditor()
    {
        var fields = PostTypeFeatureFields.ApplyFeatureContent([], enable: true);

        var created = fields.Should().ContainSingle().Which;
        created.Key.Should().Be(FeatureFieldsCatalog.ContentFieldKey);
        created.Type.Should().Be(MetaFieldType.Text);
        created.Options.GetFeatureKey().Should().Be(FeatureFieldsCatalog.Content);
        created.Options.GetEditor().Should().Be(MetaFieldEditorCatalog.BlockEditor);
    }

    [Fact]
    public void Content_Enabled_FieldExists_NoCreate()
    {
        var contentField = Field(MetaFieldType.Text, FeatureFieldsCatalog.ContentFieldKey);

        var fields = PostTypeFeatureFields.ApplyFeatureContent([contentField], enable: true);

        fields.Should().ContainSingle().Which.Id.Should().Be(contentField.Id);
    }
}
