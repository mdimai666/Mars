using Mars.Integration.Tests;
using Mars.Integration.Tests.Common;

namespace Test.Mars.MetaModelGenerator.Fixtures;

[CollectionDefinition("MetaModelGeneratorTestApp")]
public class MetaModelGeneratorAppCollection : ICollectionFixture<ApplicationFixture>
{
}


[Collection("MetaModelGeneratorTestApp")]
public class MetaModelGeneratorTests : ApplicationTests
{
    public MetaModelGeneratorTests(ApplicationFixture appFixture) : base(appFixture)
    {

    }
}
