using FluentAssertions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Data.Entities;
using Mars.Forms.Contracts;

namespace Mars.Server.Tests.Forms;

/// <summary>
/// Коды типов сквозные: <c>EMetaFieldType</c> (БД) → <c>MetaFieldType</c> (CMS) → <c>FormFieldType</c>
/// (форма) — одно и то же число, поэтому оба маппинга прямые. Новый тип в одном enum без второго
/// должен падать здесь, а не вести себя неопределённо в рантайме.
/// </summary>
public class MetaFieldTypeFormMappingTests
{
    [Fact]
    public void FormFieldType_CodesMatchMetaFieldTypes()
    {
        foreach (var type in Enum.GetValues<MetaFieldType>())
        {
            Enum.IsDefined(typeof(FormFieldType), (int)type)
                .Should().BeTrue($"код {(int)type} метаполя '{type}' должен быть и в FormFieldType");

            type.ToFormFieldType().Should().Be((FormFieldType)(int)type);
        }
    }

    [Fact]
    public void MetaFieldType_CodesMatchEntityEnum()
    {
        foreach (var type in Enum.GetValues<MetaFieldType>())
            Enum.IsDefined(typeof(EMetaFieldType), (int)type)
                .Should().BeTrue($"код {(int)type} типа '{type}' должен быть и в EMetaFieldType");
    }
}
