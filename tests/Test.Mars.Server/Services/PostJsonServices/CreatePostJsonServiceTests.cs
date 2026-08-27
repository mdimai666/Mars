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

            var createDict = (string key, JsonValue value) => new Dictionary<string, JsonNode> { [key] = value };

            yield return new object[] { createMmf(MetaFieldType.Bool, "bool1") with { Bool = true }, createDict("bool1", JsonValue.Create(true)) };
            yield return new object[] { createMmf(MetaFieldType.Int, "int1") with { Int = 123 }, createDict("int1", JsonValue.Create(123)) };
            yield return new object[] { createMmf(MetaFieldType.String, "str1") with { StringShort = "hello world!" }, createDict("str1", JsonValue.Create("hello world!")) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(MetaValuesJsonDictTestData))]
    public void JsonMetaValuesToModifyDto_ConvertJsonValuesToMetaValueDto_ShouldOk(ModifyMetaValueDetailQuery mmf, Dictionary<string, JsonNode> updateDict)
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

    [Fact]
    public void CreateJsonMetaValues_RelationArray_ShouldCreateMultiValuesWithOrder()
    {
        //Arrange
        _ = nameof(PostJsonService.CreateJsonMetaValuesToModifyDto);
        var metaField = _fixture.Create<MetaFieldDto>() with
        {
            Type = MetaFieldType.Relation,
            Key = "rel1",
            ModelName = "User",
        };
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var meta = new Dictionary<string, JsonNode>
        {
            ["rel1"] = new JsonArray(JsonValue.Create(id1)!, JsonValue.Create(id2)!),
        };

        //Act
        var modified = PostJsonService.CreateJsonMetaValuesToModifyDto(meta, [metaField], "xType");

        //Assert
        modified.Should().HaveCount(2);
        modified.Select(s => s.ModelId).Should().Equal(id1, id2);
        modified.Select(s => s.Index).Should().Equal(0, 1);
    }

    [Fact]
    public void CreateJsonMetaValues_ArrayForNonRelationField_ShouldThrow()
    {
        //Arrange
        var metaField = _fixture.Create<MetaFieldDto>() with { Type = MetaFieldType.String, Key = "str1" };
        var meta = new Dictionary<string, JsonNode>
        {
            ["str1"] = new JsonArray(JsonValue.Create("a")!),
        };

        //Act
        var act = () => PostJsonService.CreateJsonMetaValuesToModifyDto(meta, [metaField], "xType");

        //Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
