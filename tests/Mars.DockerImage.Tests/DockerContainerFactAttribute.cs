using Mars.DockerImage.Tests.Fixtures;

namespace Mars.DockerImage.Tests;

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
