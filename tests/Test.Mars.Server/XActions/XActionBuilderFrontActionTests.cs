using FluentAssertions;
using Mars.Contracts.XActions;
using Mars.XActions.Contracts;

namespace Test.Mars.Server.XActions;

public class XActionBuilderFrontActionTests
{
    private sealed class TestAct : IAct
    {
        public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
            => Task.FromResult(XActResult.ToastSuccess("ok"));
    }

    [Fact]
    public void FrontAction_BuildsWithoutHandlerOrLink()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Front").Label("Front").FrontAction();

        var command = builder.Build(out var handlerType);

        command.Type.Should().Be(XActionType.FrontAction);
        handlerType.Should().BeNull();
        command.LinkValue.Should().BeNull();
    }

    [Fact]
    public void FrontAction_CombinedWithLink_Throws()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Front").Label("Front").FrontAction().Link("/x");

        var act = () => builder.Build(out _);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FrontAction_CombinedWithHandler_Throws()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Front").Label("Front").FrontAction().Handler<TestAct>();

        var act = () => builder.Build(out _);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NoBinding_Throws_MentionsFrontAction()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.None").Label("None");

        var act = () => builder.Build(out _);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*FrontAction*");
    }
}
