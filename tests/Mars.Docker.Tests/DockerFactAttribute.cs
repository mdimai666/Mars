namespace Mars.Docker.Tests;

public class DockerFactAttribute : FactAttribute
{
    public static bool DockerTestsEnabled => Environment.GetEnvironmentVariable("MARS_DOCKER_TESTS")?.Trim() == "1";

    public DockerFactAttribute()
    {
        if (!DockerTestsEnabled)
        {
            Skip = "docker-контейнер тесты выключены; для запуска задайте MARS_DOCKER_TESTS=1";
        }
    }
}
