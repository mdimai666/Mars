namespace Mars.Integration.Tests.Common;

[CollectionDefinition(TestConstants.App, DisableParallelization = true)]
public class ApplicationCollection : ICollectionFixture<ApplicationFixture>
{
}
