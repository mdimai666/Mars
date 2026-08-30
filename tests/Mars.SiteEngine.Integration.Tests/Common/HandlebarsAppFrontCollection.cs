using Mars.SiteEngine.Integration.Tests.HandlebarsEngine;

namespace Mars.SiteEngine.Integration.Tests.Common;

[CollectionDefinition(CollectionName)]
public class HandlebarsAppFrontCollection : ICollectionFixture<HandlebarsAppFrontApplicationFixture>
{
    public const string CollectionName = "HandlebarsAppFront";
}
