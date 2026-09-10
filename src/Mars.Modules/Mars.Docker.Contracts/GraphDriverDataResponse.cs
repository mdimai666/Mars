using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class GraphDriverDataResponse // (types.GraphDriverData)
{
    [DataMember(Name = "Data", EmitDefaultValue = false)]
    public required IDictionary<string, string> Data { get; init; }

    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }
}
