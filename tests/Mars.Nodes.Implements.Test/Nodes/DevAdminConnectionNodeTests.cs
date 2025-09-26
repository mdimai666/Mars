using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Nodes;

public class DevAdminConnectionNodeTests : NodeServiceUnitTestBase
{
    private readonly IDevAdminConnectionService _devAdminConnectionService;

    public DevAdminConnectionNodeTests()
    {
        _devAdminConnectionService = Substitute.For<IDevAdminConnectionService>();
        _serviceProvider.GetService(typeof(IDevAdminConnectionService)).Returns(_devAdminConnectionService);
    }

    [Fact]
    public async Task Execute_Notify_Success()
    {
        //Arrange
        _ = nameof(DevAdminConnectionNodeImpl.Execute);
        var input = new NodeMsg() { Payload = 123 };

        var node = new DevAdminConnectionNode { Message = "ok" };

        //Act
        var msg = await ExecuteNodeEx(node, input);

        //Assert
        await _devAdminConnectionService
            .Received(1)
            .ShowNotifyMessageForAll(Arg.Any<string>());
    }
}
