using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class HealthcheckResultResponse // (types.HealthcheckResult)
{
    [DataMember(Name = "Start", EmitDefaultValue = false)]
    public required DateTime Start { get; init; }

    [DataMember(Name = "End", EmitDefaultValue = false)]
    public required DateTime End { get; init; }

    [DataMember(Name = "ExitCode", EmitDefaultValue = false)]
    public required long ExitCode { get; init; }

    [DataMember(Name = "Output", EmitDefaultValue = false)]
    public required string Output { get; init; }
}
