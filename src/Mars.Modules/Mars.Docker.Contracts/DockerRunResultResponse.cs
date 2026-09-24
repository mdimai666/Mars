namespace Mars.Docker.Contracts;

public class DockerRunResultResponse
{
    public long ExitCode { get; init; }

    public string Stdout { get; init; } = string.Empty;

    public string Stderr { get; init; } = string.Empty;

    /// <summary>Выполнение прервано по таймауту (контейнер убит).</summary>
    public bool TimedOut { get; init; }
}
