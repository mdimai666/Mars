using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class SummaryNetworkSettingsResponse // (types.SummaryNetworkSettings)
{
    [DataMember(Name = "Networks", EmitDefaultValue = false)]
    public required IDictionary<string, EndpointSettingsResponse> Networks { get; init; }
}
