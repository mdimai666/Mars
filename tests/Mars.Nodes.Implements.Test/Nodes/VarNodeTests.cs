using Mars.Nodes.Core.Nodes;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class VarNodeTests
{
    public static IEnumerable<object[]> TypesData = [
        ["int", typeof(int)],
        ["long", typeof(long)],
        ["float", typeof(float)],
        ["double", typeof(double)],
        ["decimal", typeof(decimal)],
        ["bool", typeof(bool)],
        ["string", typeof(string)],
        ["DateTime", typeof(DateTime)],
        ["Guid", typeof(Guid)],
        ["int[]", typeof(int[])],
        ["long[]", typeof(long[])],
        ["float[]", typeof(float[])],
        ["double[]", typeof(double[])],
        ["decimal[]", typeof(decimal[])],
        ["bool[]", typeof(bool[])],
        ["string[]", typeof(string[])],
        ["DateTime[]", typeof(DateTime[])],
        ["Guid[]", typeof(Guid[])],
    ];

    [Theory]
    [MemberData(nameof(TypesData))]
    public void ResolveClrType_Valid_Success(string varType, Type expectClrType)
    {
        //Arrange
        _ = nameof(VarNode.ResolveClrType);

        //Act
        var clrType = VarNode.ResolveClrType(varType);

        //Assert
        clrType.Should().Be(expectClrType);
    }

    [Fact]
    public void ResolveClrType_InvalidVarType_ShouldException()
    {
        //Arrange
        _ = nameof(VarNode.ResolveClrType);

        //Act
        var action = () => VarNode.ResolveClrType("invalid_type*");

        //Assert
        action.Should().Throw<KeyNotFoundException>();
    }

    [Theory]
    [MemberData(nameof(TypesData))]
    public void ResolveDefault_ClrType_ShouldReturnValidClrType(string varType, Type expectClrType)
    {
        //Arrange
        _ = nameof(VarNode.ResolveDefault);

        //Act
        var defaultValue = VarNode.ResolveDefault(varType);

        //Assert
        defaultValue.Should().NotBeNull();
        defaultValue.GetType().Should().Be(expectClrType);
    }

    public static IEnumerable<object[]> SetByStringData() => [
        ["int", "1", 1],
        ["int[]", "[1,2]", (int[])[1,2]],
        ["float", "1", 1f],
        ["decimal", "1", 1M],
        ["string", "\"text\"", "text"],
        ["string[]", "[\"text\"]", (string[])["text"]],
        ["bool", "false", false],
        ["bool", "true", true],
        ["DateTime", "\"1975-08-19T23:15:30.000Z\"", new DateTime(1975,8,19,23,15,30,DateTimeKind.Utc)],
        ["Guid", "\"d5acc487-a5a6-4237-9154-0b340d512ce2\"", new Guid("d5acc487-a5a6-4237-9154-0b340d512ce2")],
    ];

    [Theory]
    [MemberData(nameof(SetByStringData))]
    public void SetByString_Valid_Success(string varType, string stringValue, object expectObject)
    {
        //Arrange
        _ = nameof(VarNode.ResolveDefault);
        var node = new VarNode() { VarType = varType };

        //Act
        node.SetByJsonString(stringValue);

        //Assert
        node.Value.GetType().Should().Be(expectObject.GetType());
        node.Value.Should().BeEquivalentTo(expectObject);
    }
}
