using System.Diagnostics;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Nodes;

public class DelayNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_Delay_Success()
    {
        //Arrange
        _ = nameof(DelayNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };

        var node = new DelayNode { DelayMillis = 1000 };
        var sw = new Stopwatch();
        sw.Start();

        //Act
        var msg = await ExecuteNodeEx(node, input);
        sw.Stop();

        //Assert
        sw.ElapsedMilliseconds.Should().BeGreaterThan(node.DelayMillis);
    }
}
