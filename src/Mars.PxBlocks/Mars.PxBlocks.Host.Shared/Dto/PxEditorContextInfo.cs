namespace Mars.PxBlocks.Host.Shared.Dto;

/// <summary>Метаданные контекста редактора (GET api/PxBlocks/Contexts).</summary>
public sealed record PxEditorContextInfo
{
    public required string Name { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }
}
