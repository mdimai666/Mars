using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class TestCallBackNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_Callback_Success()
    {
        //Arrange
        _ = nameof(TestCallBackNodeImpl.Execute);

        int x = 0;
        var node = new TestCallBackNode()
        {
            Callback = () => x = 2
        };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        x.Should().Be(2);
    }
}
