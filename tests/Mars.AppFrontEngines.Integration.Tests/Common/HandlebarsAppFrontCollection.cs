using Mars.AppFrontEngines.Integration.Tests.HandlebarsEngine;

namespace Mars.AppFrontEngines.Integration.Tests.Common;

[CollectionDefinition(CollectionName)]
public class HandlebarsAppFrontCollection : ICollectionFixture<HandlebarsAppFrontApplicationFixture>
{
    public const string CollectionName = "HandlebarsAppFront";
}
