using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class HealthResponse // (types.Health)
{
    [DataMember(Name = "Status", EmitDefaultValue = false)]
    public required string Status { get; init; }

    [DataMember(Name = "FailingStreak", EmitDefaultValue = false)]
    public required long FailingStreak { get; init; }

    [DataMember(Name = "Log", EmitDefaultValue = false)]
    public required IList<HealthcheckResultResponse> Log { get; init; }
}
