namespace Mars.DockerImage.Tests.Fixtures;

/// <summary>
/// Общий <see cref="MarsFixture"/> на все Docker-тесты: сборка образа и старт контейнеров —
/// тяжёлые, поэтому выполняются один раз на коллекцию (а не на каждый тестовый класс).
/// </summary>
[CollectionDefinition(Name)]
public class MarsCollection : ICollectionFixture<MarsFixture>
{
    public const string Name = "Mars docker image";
}
