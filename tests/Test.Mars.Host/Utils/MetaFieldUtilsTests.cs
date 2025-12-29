using System.Collections;
using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.FixtureCustomizes;

namespace Test.Mars.Host.Utils;

public class MetaFieldUtilsTests
{
    public readonly IFixture _fixture = new Fixture();

    public class MetaValuesTestData : IEnumerable<object[]>
    {
        public readonly IFixture _fixture = new Fixture();

        public IEnumerator<object[]> GetEnumerator()
        {
            var dt = DateTimeOffset.Now.Date;
            var modelId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            Guid[] variantIds = [Guid.NewGuid(), Guid.NewGuid()];

            yield return new object[] { MetaFieldType.Bool, true };
            yield return new object[] { MetaFieldType.Int, 321 };
            yield return new object[] { MetaFieldType.Float, 215.1f };
            yield return new object[] { MetaFieldType.Decimal, 84m };
            yield return new object[] { MetaFieldType.Long, 666L };
            yield return new object[] { MetaFieldType.DateTime, dt };
            yield return new object[] { MetaFieldType.String, "hello" };
            yield return new object[] { MetaFieldType.Text, "world!" };
            yield return new object[] { MetaFieldType.Relation, modelId };
            yield return new object[] { MetaFieldType.Select, variantId };
            yield return new object[] { MetaFieldType.SelectMany, variantIds };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(MetaValuesTestData))]
    public void ConvertStringValueToMetaTypeObject_GeneralConverting_ShouldSuccess(MetaFieldType metaFieldType, object value)
    {
        //Arrange
        _ = nameof(MetaFieldUtils.ConvertStringValueToMetaTypeObject);

        //Act
        var converted = MetaFieldUtils.ConvertStringValueToMetaTypeObject(metaFieldType, ConvertObjectToStringPresentationOfMetaValueSetString(value));

        //Assert
        converted.Should().BeEquivalentTo(value);
    }

    [Theory]
    [ClassData(typeof(MetaValuesTestData))]
    public void MetaValueFromObject_GeneralConverting_ShouldSuccesss(MetaFieldType metaFieldType, object value)
    {
        //Arrange
        _ = nameof(MetaFieldUtils.MetaValueFromObject);
        var blank = _fixture.Create<MetaFieldDto>();

        //Act

        var modified = MetaFieldUtils.MetaValueFromObject(ModifyMetaValueDetailQuery.GetBlank(blank with { Type = metaFieldType }), value);

        //Assert
        modified.GetValueSimple().Should().BeEquivalentTo(value);
    }

    [Theory]
    [ClassData(typeof(MetaValuesTestData))]
    public void MetaFieldTypeToType_GeneralConverting_ShouldSuccesss(MetaFieldType metaFieldType, object value)
    {
        //Arrange
        _ = nameof(MetaFieldUtils.MetaFieldTypeToType);

        //Act
        var type = MetaFieldUtils.MetaFieldTypeToType(metaFieldType);

        //Assert
        type.Should().Be(value.GetType());
    }

    public class MetaValuesForJsonTestData : IEnumerable<object[]>
    {
        public readonly IFixture _fixture = new Fixture();

        public IEnumerator<object[]> GetEnumerator()
        {
            _fixture.Customize(new MetaFieldDtoCustomize());
            var blank = _fixture.Create<MetaFieldDto>();
            var createMmf = (MetaFieldType t) => ModifyMetaValueDetailQuery.GetBlank(blank with { Type = t });

            var dt = DateTimeOffset.Now.Date;
            var modelId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            Guid[] variantIds = [Guid.NewGuid(), Guid.NewGuid()];

            yield return new object[] { createMmf(MetaFieldType.Bool) with { Bool = true }, JsonValue.Create(true) };
            yield return new object[] { createMmf(MetaFieldType.Int) with { Int = 321 }, JsonValue.Create(321) };
            yield return new object[] { createMmf(MetaFieldType.Float) with { Float = 215.1f }, JsonValue.Create(215.1f) };
            yield return new object[] { createMmf(MetaFieldType.Decimal) with { Decimal = 84m }, JsonValue.Create(84m) };
            yield return new object[] { createMmf(MetaFieldType.Long) with { Long = 666L }, JsonValue.Create(666L) };
            yield return new object[] { createMmf(MetaFieldType.DateTime) with { DateTime = dt }, JsonValue.Create(dt) };
            yield return new object[] { createMmf(MetaFieldType.String) with { StringShort = "hello" }, JsonValue.Create("hello") };
            yield return new object[] { createMmf(MetaFieldType.Text) with { StringText = "world!" }, JsonValue.Create("world!") };
            yield return new object[] { createMmf(MetaFieldType.Relation) with { ModelId = modelId }, JsonValue.Create(modelId) };
            yield return new object[] { createMmf(MetaFieldType.Select) with { VariantId = variantId }, JsonValue.Create(variantId) };
            yield return new object[] { createMmf(MetaFieldType.SelectMany) with { VariantsIds = variantIds }, JsonValue.Create(variantIds)! };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(MetaValuesForJsonTestData))]
    public void SetMetaValueFromJson_GeneralConverting_ShouldSuccess(ModifyMetaValueDetailQuery mmf, JsonValue jsonValue)
    {
        //Arrange
        _ = nameof(MetaFieldUtils.MetaValueFromJson);

        //Act
        var modified = MetaFieldUtils.MetaValueFromJson(ModifyMetaValueDetailQuery.GetBlank(mmf.MetaField) with { Id = mmf.Id }, jsonValue);

        //Assert
        modified.Should().BeEquivalentTo(mmf);
    }

    [Theory]
    [ClassData(typeof(MetaValuesForJsonTestData))]
    public void MetaValueFromString_GeneralConverting_ShouldSuccess(ModifyMetaValueDetailQuery mmf, JsonValue jsonValue)
    {
        //Arrange
        _ = nameof(MetaFieldUtils.MetaValueFromString);

        //Act
        var modified = MetaFieldUtils.MetaValueFromString(
            ModifyMetaValueDetailQuery.GetBlank(mmf.MetaField) with { Id = mmf.Id },
            ConverJsonValueToValidStringPresentationOfMetaValueByMetaFieldType(mmf.MetaField.Type, jsonValue));

        //Assert
        modified.Should().BeEquivalentTo(mmf);
    }

    string ConvertObjectToStringPresentationOfMetaValueSetString(object value)
    {
        return value switch
        {
            Guid[] ids => string.Join(',', ids),
            _ => JsonValue.Create(value).ToString().Trim('\"')
        };
    }

    string ConverJsonValueToValidStringPresentationOfMetaValueByMetaFieldType(MetaFieldType metaFieldType, JsonValue jsonValue)
    {
        return metaFieldType switch
        {
            MetaFieldType.SelectMany => string.Join(',', jsonValue.GetValue<Guid[]>()),
            _ => jsonValue.ToString().Trim('\"')
        };
    }
}
