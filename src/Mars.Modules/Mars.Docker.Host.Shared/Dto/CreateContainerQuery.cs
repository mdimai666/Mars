using Mars.Docker.Contracts;

namespace Mars.Docker.Host.Shared.Dto;

public record CreateContainerQuery
{
    public required string Name { get; set; }
    public required string Platform { get; set; }
    public required string Hostname { get; set; }
    public required string Domainname { get; set; }
    public required string User { get; set; }
    public required bool AttachStdin { get; set; }
    public required bool AttachStdout { get; set; }
    public required bool AttachStderr { get; set; }
    public required IDictionary<string, EmptyStructResponse> ExposedPorts { get; set; }
    public required bool Tty { get; set; }
    public required bool OpenStdin { get; set; }
    public required bool StdinOnce { get; set; }
    public required IList<string> Env { get; set; }
    public required IList<string> Cmd { get; set; }
    //public required HealthConfig Healthcheck { get; set; }
    public required bool ArgsEscaped { get; set; }
    public required string Image { get; set; }
    public required IDictionary<string, EmptyStructResponse> Volumes { get; set; }
    public required string WorkingDir { get; set; }
    public required IList<string> Entrypoint { get; set; }
    public required bool NetworkDisabled { get; set; }
    public required string MacAddress { get; set; }
    public required IList<string> OnBuild { get; set; }
    public required IDictionary<string, string> Labels { get; set; }
    public required string StopSignal { get; set; }
    //[JsonConverter(typeof(TimeSpanSecondsConverter))]
    public required TimeSpan? StopTimeout { get; set; }
    public required IList<string> Shell { get; set; }
    //public required HostConfig HostConfig { get; set; }
    //public required NetworkingConfig NetworkingConfig { get; set; }
}
