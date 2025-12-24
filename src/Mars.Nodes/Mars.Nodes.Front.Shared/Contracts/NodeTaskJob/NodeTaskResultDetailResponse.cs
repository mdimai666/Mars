using System.Text.Json.Serialization;

namespace Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;

public record NodeTaskResultDetailResponse : NodeTaskResultSummaryResponse
{
    public required IReadOnlyCollection<NodeJobResponse> Jobs { get; init; }

    [JsonIgnore]
    public TimeSpan TaskDuration => ((EndDate ?? DateTimeOffset.Now) - StartDate);
}
