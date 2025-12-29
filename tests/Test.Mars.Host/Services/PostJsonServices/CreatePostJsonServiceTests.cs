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
