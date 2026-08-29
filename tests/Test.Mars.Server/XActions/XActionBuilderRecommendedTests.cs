using FluentAssertions;
using Mars.Contracts.XActions;
using Mars.XActions.Contracts;

namespace Test.Mars.Server.XActions;

public class XActionBuilderRecommendedTests
{
    private sealed class TestAct : IAct
    {
        public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
            => Task.FromResult(XActResult.ToastSuccess("ok"));
    }

    [Fact]
    public void Recommended_DefaultPriority_IsOne()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Rec").Label("Rec").Recommended().Handler<TestAct>();

        var command = builder.Build(out _);

        command.Recommended.Should().Be(1);
    }

    [Fact]
    public void Recommended_WithPriority_SetsValue()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Rec").Label("Rec").Recommended(10).Handler<TestAct>();

        var command = builder.Build(out _);

        command.Recommended.Should().Be(10);
    }

    [Fact]
    public void NotRecommended_IsNull()
    {
        var builder = new XActionBuilder();
        builder.Id("Test.Plain").Label("Plain").Handler<TestAct>();

        var command = builder.Build(out _);

        command.Recommended.Should().BeNull();
    }
}
