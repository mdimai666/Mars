using FluentAssertions;
using Mars.Host.Shared.Dto.Users;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Implements.Nodes.InlineFunctions;
using Mars.Nodes.Implements.Test.Services;

namespace Mars.Nodes.Implements.Test.Nodes.InlineFunctions;

public class InlineFunctionsUtilsTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task ParseAndExecute_ShouldSuccess()
    {
        //Arrange
        _ = nameof(InlineFunctionNodeImpl.Execute);
        var functions = InlineFunctionsUtilsMethodParser.ParseMethods(typeof(InlineFunctionsUtils));
        var def = functions.First(f => f.Name.EndsWith(nameof(InlineFunctionsUtils.RandomNumber)));

        var input = new NodeMsg() { Payload = 44 };
        _nodeImplementFactory.RegisterInlineFunctionNode(def);
        var node = _nodeImplementFactory.CreateInlineFunctionNode(def, ["1", "4"]);

        //Act
        var msg = await RunUsingTaskManager(node, input);

        //Assert
        ((int)msg.Payload!).Should().BeInRange(1, 4);
    }

    public static class TestUtilsClass
    {
        public static int SumFunc(int a, int b = 2)
        {
            return a + b;
        }

        public static int SumArrayFunc(int[] arr)
        {
            return arr.Sum();
        }

        public static string UserInfo(UserSummary user)
        {
            return user.FirstName;
        }
    }

    [Fact]
    public void ParseMethods_RetriveOnlyMethodWithPrimitiveParams_ShouldReturnSingleDefinition()
    {
        //Arrange
        //Act
        var functions = InlineFunctionsUtilsMethodParser.ParseMethods(typeof(TestUtilsClass), throwOnError: false);

        //Assert
        functions.Should().HaveCount(1);
    }

    [Fact]
    public void ParseMethods_IfExistNonPrimitiveArguments_ShouldException()
    {
        //Arrange
        //Act
        var action = () => InlineFunctionsUtilsMethodParser.ParseMethods(typeof(TestUtilsClass), throwOnError: true);

        //Assert
        action.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Execute_WithoutOneParam_ShouldUseDefaultValueAsync()
    {
        //Arrange
        _ = nameof(InlineFunctionNodeImpl.Execute);
        var functions = InlineFunctionsUtilsMethodParser.ParseMethods(typeof(TestUtilsClass), throwOnError: false);
        var def = functions.First(f => f.Name.EndsWith(nameof(TestUtilsClass.SumFunc)));

        _nodeImplementFactory.RegisterInlineFunctionNode(def);
        var node = _nodeImplementFactory.CreateInlineFunctionNode(def, ["3"]); // second argument default is 2

        //Act
        var msg = await RunUsingTaskManager(node);

        //Assert
        ((int)msg.Payload!).Should().Be(5);
    }
}
