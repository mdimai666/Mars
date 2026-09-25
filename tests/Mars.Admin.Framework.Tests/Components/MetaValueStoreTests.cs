using FluentAssertions;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;

namespace Mars.Admin.Framework.Tests.Components;

/// <summary>
/// Значения метаполей наружу — канонические CLR-значения общего слоя, внутрь — EAV-строки владельца:
/// проверяется соответствие типов, порядок индексов, ключи вариантов и сохранение Id строк.
/// </summary>
public class MetaValueStoreTests
{
    [Fact]
    public void GetValue_MapsRowColumnsToCanonicalValues()
    {
        var meta = Field("amount", MetaFieldType.Decimal);
        var rows = new List<MetaValueEditModel>
        {
            Row(meta, r =>
            {
                r.Decimal = 12.5m;
                r.Bool = true;
                r.Int = 7;
                r.Long = 8L;
                r.Float = 1.5d;
                r.DateTime = new DateTime(2026, 9, 11, 10, 30, 0);
                r.StringText = "текст";
                r.StringShort = "строка";
            }),
        };

        var store = new MetaValueStore(rows, [meta]);

        store.GetValue(Descriptor("amount", FormFieldType.Decimal)).Should().Be(12.5m);
        store.GetValue(Descriptor("amount", FormFieldType.Bool)).Should().Be(true);
        store.GetValue(Descriptor("amount", FormFieldType.Int)).Should().Be(7);
        store.GetValue(Descriptor("amount", FormFieldType.Long)).Should().Be(8L);
        store.GetValue(Descriptor("amount", FormFieldType.Float)).Should().Be(1.5d);
        store.GetValue(Descriptor("amount", FormFieldType.DateTime))
             .Should().Be(new DateTimeOffset(new DateTime(2026, 9, 11, 10, 30, 0)));
        store.GetValue(Descriptor("amount", FormFieldType.Text)).Should().Be("текст");
        store.GetValue(Descriptor("amount", FormFieldType.String)).Should().Be("строка");
    }

    [Fact]
    public void GetValue_WithoutRow_ReturnsEmptyDefaults()
    {
        var meta = Field("title", MetaFieldType.String);
        var store = new MetaValueStore([], [meta]);

        store.GetValue(Descriptor("title", FormFieldType.String)).Should().Be("");
        store.GetValue(Descriptor("title", FormFieldType.Bool)).Should().Be(false);
        store.GetValue(Descriptor("title", FormFieldType.DateTime)).Should().BeNull();
        store.GetValue(Descriptor("title", FormFieldType.Relation)).Should().Be(Guid.Empty);
        store.GetList(Descriptor("title", FormFieldType.String, multiple: true)).Should().BeEmpty();
    }

    [Fact]
    public void GetValue_SelectAndSelectMany_MapVariantIdsToKeys()
    {
        var first = (Id: Guid.NewGuid(), Key: "draft");
        var second = (Id: Guid.NewGuid(), Key: "live");
        var meta = Field("state", MetaFieldType.SelectMany, variants: [first, second]);

        var rows = new List<MetaValueEditModel>
        {
            Row(meta, r => r.VariantsIds = [first.Id, second.Id]),
        };

        var store = new MetaValueStore(rows, [meta]);

        store.GetList(Descriptor("state", FormFieldType.SelectMany))
             .Should().Equal("draft", "live");
    }

    [Fact]
    public void SetValue_CreatesRow_AndKeepsRowIdOnNextWrite()
    {
        var meta = Field("title", MetaFieldType.String);
        var rows = new List<MetaValueEditModel>();
        var store = new MetaValueStore(rows, [meta]);
        var field = Descriptor("title", FormFieldType.String);

        store.SetValue(field, "первое");

        rows.Should().ContainSingle();
        var id = rows[0].Id;
        id.Should().NotBe(Guid.Empty);
        rows[0].StringShort.Should().Be("первое");
        rows[0].Index.Should().Be(0);

        store.SetValue(field, "второе");

        rows.Should().ContainSingle("строка значения одна");
        rows[0].Id.Should().Be(id, "Id строки сохраняется при повторной записи");
        rows[0].StringShort.Should().Be("второе");
    }

    [Fact]
    public void SetValue_Select_WritesVariantIdByKey()
    {
        var draft = (Id: Guid.NewGuid(), Key: "draft");
        var meta = Field("state", MetaFieldType.Select, variants: [draft]);
        var rows = new List<MetaValueEditModel>();
        var store = new MetaValueStore(rows, [meta]);

        store.SetValue(Descriptor("state", FormFieldType.Select), "draft");

        rows.Should().ContainSingle();
        rows[0].VariantId.Should().Be(draft.Id);
    }

    [Fact]
    public void SetList_KeepsRowsByIndex_AndDropsExtraOnes()
    {
        var meta = Field("tags", MetaFieldType.Text, multiple: true);
        var rows = new List<MetaValueEditModel> { Row(meta, r => r.StringText = "старое"), Row(meta, r => r.Index = 1) };
        var ids = rows.Select(r => r.Id).ToArray();
        var store = new MetaValueStore(rows, [meta]);
        var field = Descriptor("tags", FormFieldType.Text, multiple: true);

        store.SetList(field, ["одно", "два", "три"]);

        rows.Should().HaveCount(3);
        rows.Select(r => r.Index).Should().Equal(0, 1, 2);
        rows.Select(r => r.StringText).Should().Equal("одно", "два", "три");
        rows[0].Id.Should().Be(ids[0]);
        rows[1].Id.Should().Be(ids[1]);

        store.SetList(field, ["одно"]);

        rows.Should().ContainSingle();
        rows[0].StringText.Should().Be("одно");
    }

    [Fact]
    public void SetValue_SelectMany_WritesVariantIds()
    {
        var live = (Id: Guid.NewGuid(), Key: "live");
        var meta = Field("state", MetaFieldType.SelectMany, variants: [live]);
        var rows = new List<MetaValueEditModel>();
        var store = new MetaValueStore(rows, [meta]);

        store.SetList(Descriptor("state", FormFieldType.SelectMany), ["live"]);

        rows.Should().ContainSingle();
        rows[0].VariantsIds.Should().Equal(live.Id);
    }

    [Fact]
    public void GetValue_BlankNullableRelationRow_IsNotAValue()
    {
        var meta = Field("photo", MetaFieldType.Relation);
        var rows = new List<MetaValueEditModel> { Row(meta) };
        var store = new MetaValueStore(rows, [meta]);

        store.GetValue(Descriptor("photo", FormFieldType.Relation)).Should().Be(Guid.Empty);
        rows.Should().BeEmpty();
    }

    [Fact]
    public void SetValue_ClearingNullableRelation_RemovesRow()
    {
        var meta = Field("photo", MetaFieldType.Relation);
        var rows = new List<MetaValueEditModel> { Row(meta, r => r.ModelId = Guid.NewGuid()) };
        var store = new MetaValueStore(rows, [meta]);

        store.SetValue(Descriptor("photo", FormFieldType.Relation), null);

        rows.Should().BeEmpty();
    }

    [Fact]
    public void SetValue_RequiredRelation_KeepsEmptyRowForValidation()
    {
        var meta = Field("photo", MetaFieldType.Relation);
        var rows = new List<MetaValueEditModel>();
        var store = new MetaValueStore(rows, [meta]);

        store.SetValue(Descriptor("photo", FormFieldType.Relation, required: true), null);

        rows.Should().ContainSingle();
        rows[0].ModelId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void SetList_BlankIdsOfNullableRelation_AreNotStored()
    {
        var meta = Field("photos", MetaFieldType.Relation, multiple: true);
        var rows = new List<MetaValueEditModel>();
        var store = new MetaValueStore(rows, [meta]);
        var id = Guid.NewGuid();

        store.SetList(Descriptor("photos", FormFieldType.Relation, multiple: true), [id, Guid.Empty, null]);

        rows.Should().ContainSingle();
        rows[0].ModelId.Should().Be(id);
    }

    //=====================================

    static FormFieldDescriptor Descriptor(string key, FormFieldType type, bool multiple = false, bool required = false)
        => new() { Key = key, Title = key, Type = type, Multiple = multiple, Required = required };

    static MetaFieldEditModel Field(string key, MetaFieldType type, bool multiple = false,
                                    params (Guid Id, string Key)[] variants)
    {
        var field = new MetaFieldEditModel { Id = Guid.NewGuid(), Key = key, Title = key, Type = type, IsMultiple = multiple };

        field.Variants.AddRange(variants.Select(variant => new MetaFieldVariantEditModel
        {
            Id = variant.Id,
            Key = variant.Key,
            Title = variant.Key,
        }));

        return field;
    }

    static MetaValueEditModel Row(MetaFieldEditModel meta, Action<MetaValueEditModel>? setup = null)
    {
        var row = new MetaValueEditModel { Id = Guid.NewGuid(), MetaField = meta };
        setup?.Invoke(row);
        return row;
    }
}
