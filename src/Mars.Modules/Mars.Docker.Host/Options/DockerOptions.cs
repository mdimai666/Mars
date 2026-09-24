namespace Mars.Docker.Host.Options;

/// <summary>
/// Подключение к Docker-демону и источнику образов. Задаётся в <c>appsettings.json</c>
/// секцией <see cref="SectionName"/>; из админки не редактируется.
/// </summary>
public class DockerOptions
{
    public const string SectionName = "Docker";

    /// <summary>
    /// Endpoint демона (<c>npipe://./pipe/docker_engine</c>, <c>unix:///var/run/docker.sock</c>,
    /// <c>tcp://host:2375</c>). Пусто — стандартный локальный сокет платформы.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Источник образов (registry/mirror), например <c>mirror.gcr.io</c>.
    /// Пусто — Docker Hub. Не применяется к именам, уже содержащим registry.
    /// </summary>
    public string RegistryUrl { get; set; } = string.Empty;

    public string RegistryUser { get; set; } = string.Empty;

    public string RegistryPassword { get; set; } = string.Empty;

    /// <summary>Таймаут RunOnce/Exec по умолчанию, если в запросе не задан.</summary>
    public int DefaultRunTimeoutSeconds { get; set; } = 300;
}
