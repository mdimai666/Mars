using System.Text.Json.Serialization;

namespace Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;

public record NodeJobExecutionTimeResponse
{
    public required Guid JobGuid { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset? End { get; init; }
    public required NodeJobExecutionResultResponse Result { get; init; }
    public required NodeJobExecutionProblemDetailResponse? Exception { get; init; }

    [JsonIgnore]
    public TimeSpan TaskDuration => ((End ?? DateTimeOffset.Now) - Start);

    [JsonIgnore]
    public string TaskDurationTotalMillisecondsText => TaskDuration.TotalMilliseconds.ToString("0.0") + "ms";
}
