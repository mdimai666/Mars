using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Contexts;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;

namespace Mars.Integration.Tests;

[Collection(TestConstants.App)]
public abstract class ApplicationTests
{
    protected readonly ApplicationFixture AppFixture;
    protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    public IFixture _fixture = new Fixture();

    protected ApplicationTests(ApplicationFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();

        // Из-за способа хранения и округления DateTime, оно может на миллисекунды отличаться
        AssertionOptions.AssertEquivalencyUsing(
            options => options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Subject, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTime>()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Subject, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTimeOffset>()
        );

        //AppFixture.MessageQueueFixture.ClearTopics().RunSync();
    }

}
