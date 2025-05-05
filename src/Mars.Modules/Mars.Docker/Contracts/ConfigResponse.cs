using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ConfigResponse // (container.Config)
{
    [DataMember(Name = "Hostname", EmitDefaultValue = false)]
    public required string Hostname { get; init; }

    [DataMember(Name = "Domainname", EmitDefaultValue = false)]
    public required string Domainname { get; init; }

    [DataMember(Name = "User", EmitDefaultValue = false)]
    public required string User { get; init; }

    [DataMember(Name = "AttachStdin", EmitDefaultValue = false)]
    public required bool AttachStdin { get; init; }

    [DataMember(Name = "AttachStdout", EmitDefaultValue = false)]
    public required bool AttachStdout { get; init; }

    [DataMember(Name = "AttachStderr", EmitDefaultValue = false)]
    public required bool AttachStderr { get; init; }

    [DataMember(Name = "ExposedPorts", EmitDefaultValue = false)]
    public required IDictionary<string, EmptyStructResponse> ExposedPorts { get; init; }

    [DataMember(Name = "Tty", EmitDefaultValue = false)]
    public required bool Tty { get; init; }

    [DataMember(Name = "OpenStdin", EmitDefaultValue = false)]
    public required bool OpenStdin { get; init; }

    [DataMember(Name = "StdinOnce", EmitDefaultValue = false)]
    public required bool StdinOnce { get; init; }

    [DataMember(Name = "Env", EmitDefaultValue = false)]
    public required IList<string> Env { get; init; }

    [DataMember(Name = "Cmd", EmitDefaultValue = false)]
    public required IList<string> Cmd { get; init; }

    [DataMember(Name = "Healthcheck", EmitDefaultValue = false)]
    public required HealthConfigResponse Healthcheck { get; init; }

    [DataMember(Name = "ArgsEscaped", EmitDefaultValue = false)]
    public required bool ArgsEscaped { get; init; }

    [DataMember(Name = "Image", EmitDefaultValue = false)]
    public required string Image { get; init; }

    [DataMember(Name = "Volumes", EmitDefaultValue = false)]
    public required IDictionary<string, EmptyStructResponse> Volumes { get; init; }

    [DataMember(Name = "WorkingDir", EmitDefaultValue = false)]
    public required string WorkingDir { get; init; }

    [DataMember(Name = "Entrypoint", EmitDefaultValue = false)]
    public required IList<string> Entrypoint { get; init; }

    [DataMember(Name = "NetworkDisabled", EmitDefaultValue = false)]
    public required bool NetworkDisabled { get; init; }

    [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
    public required string MacAddress { get; init; }

    [DataMember(Name = "OnBuild", EmitDefaultValue = false)]
    public required IList<string> OnBuild { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IDictionary<string, string> Labels { get; init; }

    [DataMember(Name = "StopSignal", EmitDefaultValue = false)]
    public required string StopSignal { get; init; }

    [DataMember(Name = "StopTimeout", EmitDefaultValue = false)]
    //[JsonConverter(typeof(TimeSpanSecondsConverter))]
    public required TimeSpan? StopTimeout { get; init; }

    [DataMember(Name = "Shell", EmitDefaultValue = false)]
    public required IList<string> Shell { get; init; }
}
