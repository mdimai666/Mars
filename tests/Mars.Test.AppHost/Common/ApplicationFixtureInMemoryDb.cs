namespace Mars.Integration.Tests.Common;

public class ApplicationFixtureInMemoryDb : ApplicationFixture
{
    public override IDatabaseFixture DbFixture { get; } = new InMemoryDatabaseFixture();
}
