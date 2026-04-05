namespace Mars.Nodes.Core;

public record ExecutionParameters(
    Guid TaskId,
    Guid JobGuid,
    int InputPort,
    CancellationToken CancellationToken,
    int SourceOutputPort,
    bool IsDebugMode = false
);
