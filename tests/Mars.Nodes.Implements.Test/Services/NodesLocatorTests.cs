using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Services;

public class NodesLocatorTests : NodeServiceUnitTestBase
{
    [Fact]
    public void CreateExamplesList_ShouldReturnExamples()
    {
        //Arrange
        //Act
        var dict = _nodesLocator.CreateExamplesList();

        //Assert
        dict.Should().HaveCountGreaterThan(0);
        var item = dict.First();
        item.Name.Should().NotBeNullOrEmpty();
        item.Description.Should().NotBeNullOrEmpty();
        item.NodeType.Should().NotBeNull();
        item.ExampleHandlerInstance.Should().NotBeNull();

        var examples = item.ExampleHandlerInstance.Handle();
        examples.Should().HaveCountGreaterThan(0);
    }
}
