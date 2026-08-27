using FluentAssertions;
using Mars.Data.OwnedTypes.MetaFields;
using Mars.Data.Repositories;

namespace Test.Mars.Server.Utils;

public class MetaFieldsToolsTests
{
    static MetaFieldVariant Variant(string title = "Title", string key = "")
        => new() { Id = Guid.NewGuid(), Title = title, Key = key };

    [Fact]
    public void EnsureVariantKeys_GeneratesKeyFromTitle_WhenKeyIsEmpty()
    {
        var variants = new List<MetaFieldVariant> { Variant("Variant One") };

        MetaFieldsTools.EnsureVariantKeys(variants);

        variants[0].Key.Should().Be("variant_one");
    }

    [Fact]
    public void EnsureVariantKeys_NormalizesExplicitKey()
    {
        var variants = new List<MetaFieldVariant> { Variant("T", "My-Key 2") };

        MetaFieldsTools.EnsureVariantKeys(variants);

        variants[0].Key.Should().Be("my_key_2");
    }

    [Fact]
    public void EnsureVariantKeys_FallsBackToId_WhenTitleNotConvertible()
    {
        var variants = new List<MetaFieldVariant> { Variant("Привет") };

        MetaFieldsTools.EnsureVariantKeys(variants);

        variants[0].Key.Should().Be($"variant_{variants[0].Id:N}");
    }

    [Fact]
    public void EnsureVariantKeys_ResolvesCollisions_WithSuffixes()
    {
        var variants = new List<MetaFieldVariant>
        {
            Variant("Same Title"),
            Variant("Same Title"),
            Variant("Same Title"),
        };

        MetaFieldsTools.EnsureVariantKeys(variants);

        variants.Select(v => v.Key).Should().BeEquivalentTo(["same_title", "same_title_2", "same_title_3"],
            o => o.WithStrictOrdering());
    }

    [Fact]
    public void EnsureVariantKeys_EmptyList_NoThrow()
    {
        var act = () => MetaFieldsTools.EnsureVariantKeys([]);

        act.Should().NotThrow();
    }
}
