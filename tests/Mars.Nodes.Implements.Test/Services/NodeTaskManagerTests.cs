using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Implements.Test.Services;

public class NodeTaskManagerTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task CurrentTasks_HaveCount_ShouldCreateTaskAndReturnCount()
    {
        //Arrange
        var input = new NodeMsg() { Payload = 123 };
        var node = new DelayNode { DelayMillis = 100 };

        //Act
        _ /*we are not waiting*/ = RunUsingTaskManager(node, input);
        await Task.Delay(10);

        //Assert
        var currentTasks = _nodeTaskManager.CurrentTasks();
        currentTasks.Count.Should().Be(1);
        _nodeTaskManager.CompletedTasks().Count.Should().Be(0);
    }

    [Fact]
    public async Task CompletedTasks_HaveCount_ShouldCreateTaskAndReturnCount()
    {
        //Arrange
        var input = new NodeMsg() { Payload = 123 };
        var node = new DelayNode { DelayMillis = 100 };

        //Act
        await RunUsingTaskManager(node, input);
        await Task.Delay(10);

        //Assert
        var currentTasks = _nodeTaskManager.CurrentTasks();
        currentTasks.Count.Should().Be(0);
        _nodeTaskManager.CompletedTasks().Count.Should().Be(1);
    }

}
