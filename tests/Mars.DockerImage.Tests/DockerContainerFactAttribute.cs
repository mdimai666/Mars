using Mars.DockerContainer.Tests.Fixtures;

namespace Mars.DockerContainer.Tests;

public class DockerContainerFactAttribute : FactAttribute
{
    public DockerContainerFactAttribute()
    {
        if (!MarsFixture.DockerTestsEnabled)
        {
            Skip = "docker-контейнер тесты выключены; для запуска задайте MARS_DOCKER_TESTS=1";
        }
    }
}
