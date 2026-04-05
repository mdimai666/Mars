using System.Text;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.StringFunctions;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes;

public class StringNodeTests : NodeServiceUnitTestBase
{
    public StringNodeTests()
    {
        // Чтобы Encoding.GetEncoding работал в современном .NET (Core/5+), нужно один раз при старте приложения выполнить:
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task Execute_ToUpper_Success()
    {
        //Arrange
        _ = nameof(StringNodeImpl.Execute);
        var input = new NodeMsg() { Payload = "expect" };
        var node = new StringNode { Operations = [new() { Method = nameof(StringNodeOperationUtils.ToUpper) }] };

        //Act
        var result = await ExecuteNode(node, input);

        //Assert
        result.Payload.Should().BeEquivalentTo("EXPECT");
    }

    [Theory]
    [ClassData(typeof(StringNodeOperationTestData))]
    public async Task Execute_AllOperation_Success(string methodName, object inputValue, object expected, object[]? arguments = null)
    {
        //Arrange
        _ = nameof(StringNodeImpl.Execute);
        var input = new NodeMsg() { Payload = inputValue };
        var node = new StringNode { Operations = [new() { Method = methodName, ParameterValues = arguments?.Select(s => s?.ToString()!).ToArray() ?? [] }] };

        //Act
        var result = await ExecuteNode(node, input);

        //Assert
        result.Payload.Should().BeEquivalentTo(expected);
    }
}

public partial class StringNodeUtilTests
{
    private Dictionary<string, StringMethod> _functions;

    public StringNodeUtilTests()
    {
        _functions = StringNodeOperationUtilsMethodParser.ParseMethods(typeof(StringNodeOperationUtils)).ToDictionary(s => s.Name);
        // Чтобы Encoding.GetEncoding работал в современном .NET (Core/5+), нужно один раз при старте приложения выполнить:
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void ToUpper_ShouldSuccess()
    {
        _ = nameof(StringNodeOperationUtilsMethodParser);
        var operation = new StringOperation() { Method = nameof(StringNodeOperationUtils.ToUpper) };

        var result = StringNodeImpl.ExecuteOperations("text", [operation], _functions);

        result.Should().Be("TEXT");
    }

    [Theory]
    [ClassData(typeof(StringNodeOperationTestData))]

    public void TestAllMethods(string methodName, object input, object expected, object[]? arguments = null)
    {
        //Arrange
        var operation = new StringOperation() { Method = methodName, ParameterValues = arguments! };

        //Act
        var result = StringNodeImpl.ExecuteOperations(input, [operation], _functions);

        //Assert
        result.Should().BeEquivalentTo(expected);
    }
}
