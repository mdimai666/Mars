using AutoFixture;
using FluentAssertions;
using Mars.Data.Contexts;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.Integration.Tests;

[Collection(TestConstants.App)]
public abstract class ApplicationTests
{
    protected readonly ApplicationFixture AppFixture;
    protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    private readonly Lazy<IFixture> _fixtureLazy;

    /// <summary>
    /// Фикстура AutoFixture, заранее кастомизированная ссылками на сид-сущности БД
    /// текущей фикстуры (<see cref="ApplicationFixture.Catalog"/>). Создаётся лениво —
    /// каталог наполняется в Seed до первого обращения.
    /// </summary>
    public IFixture _fixture => _fixtureLazy.Value;

    protected ApplicationTests(ApplicationFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();

        _fixtureLazy = new Lazy<IFixture>(() =>
        {
            var fixture = new Fixture();
            fixture.Customize(new FixtureCustomize(AppFixture.Catalog));
            return fixture;
        });

        // Из-за способа хранения и округления DateTime, оно может на миллисекунды отличаться
        AssertionOptions.AssertEquivalencyUsing(
            options => options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTime>()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTimeOffset>()
        );

        //AppFixture.MessageQueueFixture.ClearTopics().RunSync();
    }

}
