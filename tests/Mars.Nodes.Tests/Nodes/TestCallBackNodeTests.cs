using FluentAssertions;
using Mars.Nodes.Tests.NodesForTesting;
using Mars.Nodes.Tests.Services;

namespace Mars.Nodes.Tests.Nodes;

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
            Callback = (_, _) => x = 2
        };

        //Act
        var msg = await ExecuteNode(node);

        //Assert
        x.Should().Be(2);
    }
}
