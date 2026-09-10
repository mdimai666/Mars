using FluentAssertions;
using Mars.Nodes.Core.Examples.Nodes;
using Mars.Nodes.Core.Implements.Nodes.TaskNodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Tests.Services;

namespace Mars.Nodes.Tests.Nodes;

public class KillTaskJobNodeTests : NodeServiceUnitTestBase
{
    /// <summary>
    /// Тут проблема. При порождении задачи KillTaskJobNode создает в том же контексте, и это вызывает проблемы scope. Это надо подумать.
    /// </summary>
    [Fact]
    public async Task Execute_TerminateJobs_SucceedsWithoutException()
    {
        //Arrange
        _ = nameof(KillTaskJobNodeImpl.Execute);
        var nodes = new KillTaskJobNodeExample1().Handle(null!);

        //Act
        var action = () => RunUsingTaskManager(NodesWorkflowBuilder.Create().Add(nodes.ToArray()));

        //Assert
        await action.Should().NotThrowAsync();
    }
}
