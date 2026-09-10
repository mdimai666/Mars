using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class RestartPolicyResponse
{
    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required RestartPolicyKindResponse Name { get; init; }

    [DataMember(Name = "MaximumRetryCount", EmitDefaultValue = false)]
    public required long MaximumRetryCount { get; init; }
}
