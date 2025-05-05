using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class HealthConfigResponse // (container.HealthConfig)
{
    [DataMember(Name = "Test", EmitDefaultValue = false)]
    public required IList<string> Test { get; init; }

    [DataMember(Name = "Interval", EmitDefaultValue = false)]
    //[JsonConverter(typeof(TimeSpanNanosecondsConverter))]
    public required TimeSpan Interval { get; init; }

    [DataMember(Name = "Timeout", EmitDefaultValue = false)]
    //[JsonConverter(typeof(TimeSpanNanosecondsConverter))]
    public required TimeSpan Timeout { get; init; }

    [DataMember(Name = "StartPeriod", EmitDefaultValue = false)]
    public required long StartPeriod { get; init; }

    [DataMember(Name = "Retries", EmitDefaultValue = false)]
    public required long Retries { get; init; }
}
