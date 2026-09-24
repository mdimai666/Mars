using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ImageSummaryResponse1 // (types.ImageSummary)
{
    [DataMember(Name = "Id", EmitDefaultValue = false)]
    public required string ID { get; init; }

    [DataMember(Name = "ParentId", EmitDefaultValue = false)]
    public required string ParentID { get; init; }

    [DataMember(Name = "RepoTags", EmitDefaultValue = false)]
    public required IList<string> RepoTags { get; init; }

    [DataMember(Name = "RepoDigests", EmitDefaultValue = false)]
    public required IList<string> RepoDigests { get; init; }

    [DataMember(Name = "Created", EmitDefaultValue = false)]
    public required DateTime Created { get; init; }

    [DataMember(Name = "Size", EmitDefaultValue = false)]
    public required long Size { get; init; }

    [DataMember(Name = "SharedSize", EmitDefaultValue = false)]
    public required long SharedSize { get; init; }

    [DataMember(Name = "VirtualSize", EmitDefaultValue = false)]
    public required long VirtualSize { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    [DataMember(Name = "Containers", EmitDefaultValue = false)]
    public required long Containers { get; init; }
}
