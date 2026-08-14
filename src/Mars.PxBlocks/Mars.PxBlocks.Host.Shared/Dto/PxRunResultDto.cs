namespace Mars.PxBlocks.Host.Shared.Dto;

/// <summary>Итог серверного исполнения (событие хаба PxBlocksHubMethods.RunFinished).</summary>
public sealed record PxRunResultDto
{
    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>Блок, на котором исполнение упало — подсветить в редакторе.</summary>
    public string? ErrorBlockId { get; init; }

    public bool Canceled { get; init; }

    public long Steps { get; init; }
}
