using System.Collections;
using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.FixtureCustomizes;

namespace Test.Mars.Host.Services.PostJsonServices;

public sealed class CreatePostJsonServiceTests : PostJsonServiceTestBase
{
    public class MetaValuesForJsonTestData : IEnumerable<object[]>
    {
        public readonly IFixture _fixture = new Fixture();

        public IEnumerator<object[]> GetEnumerator()
        {
            _fixture.Customize(new MetaFieldDtoCustomize());
            var blank = _fixture.Create<MetaFieldDto>();
            var createMmf = (MetaFieldType t) => ModifyMetaValueDetailQuery.GetBlank(blank with { Type = t });

            var dt = DateTime.Now;
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
        _ = nameof(PostJsonService.UpdateMetaValueFromJson);

        //Act
        var modified = PostJsonService.UpdateMetaValueFromJson(ModifyMetaValueDetailQuery.GetBlank(mmf.MetaField) with { Id = mmf.Id }, jsonValue);

        //Assert
        modified.Should().BeEquivalentTo(mmf);
    }

    public class MetaValuesJsonDictTestData : IEnumerable<object[]>
    {
        public readonly IFixture _fixture = new Fixture();

        public IEnumerator<object[]> GetEnumerator()
        {
            _fixture.Customize(new MetaFieldDtoCustomize());
            var blank = _fixture.Create<MetaFieldDto>();
            var createMmf = (MetaFieldType t, string key) => ModifyMetaValueDetailQuery.GetBlank(blank with { Type = t, Key = key });

            var createDict = (string key, JsonValue value) => new Dictionary<string, JsonValue> { [key] = value };

            yield return new object[] { createMmf(MetaFieldType.Bool, "bool1") with { Bool = true }, createDict("bool1", JsonValue.Create(true)) };
            yield return new object[] { createMmf(MetaFieldType.Int, "int1") with { Int = 123 }, createDict("int1", JsonValue.Create(123)) };
            yield return new object[] { createMmf(MetaFieldType.String, "str1") with { StringShort = "hello world!" }, createDict("str1", JsonValue.Create("hello world!")) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(MetaValuesJsonDictTestData))]
    public void JsonMetaValuesToModifyDto_ConvertJsonValuesToMetaValueDto_ShouldOk(ModifyMetaValueDetailQuery mmf, Dictionary<string, JsonValue> updateDict)
    {
        //Arrange
        _ = nameof(PostJsonService.CreateJsonMetaValuesToModifyDto);

        //Act
        var modified = PostJsonService.CreateJsonMetaValuesToModifyDto(updateDict, [mmf.MetaField], "xType");

        //Assert
        modified.First().Should().BeEquivalentTo(mmf, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<ModifyMetaValueDetailQuery>()
            .Excluding(s => s.Id)
            .ExcludingMissingMembers());
    }
}
