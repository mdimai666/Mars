using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ContainerStateResponse // (types.ContainerState)
{
    [DataMember(Name = "Status", EmitDefaultValue = false)]
    public required string Status { get; init; }

    [DataMember(Name = "Running", EmitDefaultValue = false)]
    public required bool Running { get; init; }

    [DataMember(Name = "Paused", EmitDefaultValue = false)]
    public required bool Paused { get; init; }

    [DataMember(Name = "Restarting", EmitDefaultValue = false)]
    public required bool Restarting { get; init; }

    [DataMember(Name = "OOMKilled", EmitDefaultValue = false)]
    public required bool OOMKilled { get; init; }

    [DataMember(Name = "Dead", EmitDefaultValue = false)]
    public required bool Dead { get; init; }

    [DataMember(Name = "Pid", EmitDefaultValue = false)]
    public required long Pid { get; init; }

    [DataMember(Name = "ExitCode", EmitDefaultValue = false)]
    public required long ExitCode { get; init; }

    [DataMember(Name = "Error", EmitDefaultValue = false)]
    public required string Error { get; init; }

    [DataMember(Name = "StartedAt", EmitDefaultValue = false)]
    public required string StartedAt { get; init; }

    [DataMember(Name = "FinishedAt", EmitDefaultValue = false)]
    public required string FinishedAt { get; init; }

    [DataMember(Name = "Health", EmitDefaultValue = false)]
    public required HealthResponse Health { get; init; }
}
