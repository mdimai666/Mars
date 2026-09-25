using FluentAssertions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Resources;

namespace Mars.Server.Tests.Dto;

public class PostTypeGridColumnsTests
{
    static MetaFieldDetailResponse Field(MetaFieldType type, string key, string? title = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title ?? key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = false,
            Default = null,
            Options = null,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            ModelName = null,
            Variants = null,
        };

    static PostTypeGridColumn Configured(string key, bool visible = true) => new() { Key = key, Visible = visible };

    static readonly string[] AllFeatures =
    [
        PostTypeConstants.Features.Category,
        PostTypeConstants.Features.Status,
        PostTypeConstants.Features.Excerpt,
        PostTypeConstants.Features.Tags,
        PostTypeConstants.Features.Language,
        PostTypeConstants.Features.ModifyCreatedDate,
        PostTypeConstants.Features.PostImage,
    ];

    [Fact]
    public void Available_WithoutFeatures_KeepsUngatedBaseColumns()
    {
        // created_at в гриде не гейтится ModifyCreatedDate — в отличие от слота формы
        var keys = PostTypeGridColumns.Available([], []).Select(c => c.Key);

        keys.Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
    }

    [Fact]
    public void Available_WithCategoryAndStatus_KeepsDefaultBaseOrder()
    {
        var features = new[] { PostTypeConstants.Features.Category, PostTypeConstants.Features.Status };

        var keys = PostTypeGridColumns.Available(features, []).Select(c => c.Key);

        keys.Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Categories,
            SystemFieldsCatalog.Status,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
    }

    [Fact]
    public void Available_WithAllFeatures_DoesNotLeakFormOnlySlots()
    {
        var keys = PostTypeGridColumns.Available(AllFeatures, []).Select(c => c.Key);

        keys.Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Categories,
            SystemFieldsCatalog.Status,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
    }

    [Fact]
    public void Available_BaseColumns_AreSystemAndTakeTitleFromResourceKey()
    {
        var columns = PostTypeGridColumns.Available([], []);

        columns.Should().OnlyContain(c => c.IsSystem && c.Visible);
        columns.Single(c => c.Key == SystemFieldsCatalog.Title).TitleKey.Should().Be(nameof(AppRes.Title));
        columns.Single(c => c.Key == SystemFieldsCatalog.Title).Title.Should().Be(AppRes.Title);
        columns.Single(c => c.Key == SystemFieldsCatalog.CreatedAt).Title.Should().Be(AppRes.CreatedAt);
    }

    [Fact]
    public void Available_AppendsMetaFieldsAfterBaseColumns()
    {
        var fields = new[]
        {
            Field(MetaFieldType.String, "subtitle", "Подзаголовок"),
            Field(MetaFieldType.Image, "cover"),
        };

        var columns = PostTypeGridColumns.Available([], fields);

        columns.Select(c => c.Key).Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt,
            "subtitle",
            "cover");

        var meta = columns.Single(c => c.Key == "subtitle");
        meta.IsSystem.Should().BeFalse();
        meta.TitleKey.Should().BeNull();
        meta.Title.Should().Be("Подзаголовок");
    }

    [Fact]
    public void Available_SkipsQueryAndSelectManyMetaFields()
    {
        var fields = new[]
        {
            Field(MetaFieldType.SelectMany, "tags_many"),
            Field(MetaFieldType.Query, "computed"),
        };

        var columns = PostTypeGridColumns.Available([], fields);

        columns.Should().OnlyContain(c => c.IsSystem);
    }

    [Fact]
    public void BaseColumns_MatchDefaultGridOrder()
    {
        PostTypeGridConstants.BaseColumns.Should().Equal(PostTypeGridColumns.BaseKeys);
        PostTypeGridConstants.BaseColumns.Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Categories,
            SystemFieldsCatalog.Status,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
    }

    [Fact]
    public void Merge_WithoutSettings_ReturnsAvailableOrderVisible()
    {
        var available = PostTypeGridColumns.Available([], [Field(MetaFieldType.String, "subtitle")]);

        var merged = PostTypeGridColumns.Merge(null, available);

        merged.Should().Equal(available);
        merged.Should().OnlyContain(c => c.Visible);
    }

    [Fact]
    public void Merge_PutsConfiguredFirst_AndAppendsUnconfiguredAtTheEnd()
    {
        var available = PostTypeGridColumns.Available([], [Field(MetaFieldType.String, "subtitle")]);

        var merged = PostTypeGridColumns.Merge(
            [Configured(SystemFieldsCatalog.CreatedAt), Configured("subtitle")], available);

        merged.Select(c => c.Key).Should().Equal(
            SystemFieldsCatalog.CreatedAt,
            "subtitle",
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Author);
        merged.Should().OnlyContain(c => c.Visible);
    }

    [Fact]
    public void Merge_DropsUnknownConfiguredKeys()
    {
        var available = PostTypeGridColumns.Available([], []);

        var merged = PostTypeGridColumns.Merge(
            [Configured("gone"), Configured(SystemFieldsCatalog.Title)], available);

        merged.Select(c => c.Key).Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
    }

    [Fact]
    public void Merge_KeepsHiddenConfiguredColumn_InPlaceAndInvisible()
    {
        var available = PostTypeGridColumns.Available([], []);

        var merged = PostTypeGridColumns.Merge([Configured(SystemFieldsCatalog.Author, visible: false)], available);

        merged.Select(c => c.Key).Should().Equal(
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.CreatedAt);
        merged[0].Visible.Should().BeFalse();
        merged.Skip(1).Should().OnlyContain(c => c.Visible);
    }

    [Fact]
    public void Merge_DuplicatedConfiguredKey_TakesFirstOccurrence()
    {
        var available = PostTypeGridColumns.Available([], []);

        var merged = PostTypeGridColumns.Merge(
            [Configured(SystemFieldsCatalog.Title, visible: false), Configured(SystemFieldsCatalog.Title)],
            available);

        merged.Select(c => c.Key).Should().Equal(
            SystemFieldsCatalog.Title,
            SystemFieldsCatalog.Author,
            SystemFieldsCatalog.CreatedAt);
        merged[0].Visible.Should().BeFalse();
    }

    [Fact]
    public void Merge_DoesNotMutateAvailableColumns()
    {
        var available = PostTypeGridColumns.Available([], []);

        PostTypeGridColumns.Merge([Configured(SystemFieldsCatalog.Title, visible: false)], available);

        available.Should().OnlyContain(c => c.Visible);
    }
}
