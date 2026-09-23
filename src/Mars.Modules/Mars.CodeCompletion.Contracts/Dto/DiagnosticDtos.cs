namespace Mars.CodeCompletion.Contracts.Dto;

public class DiagnosticDto
{
    public int OffsetFrom { get; set; }

    public int OffsetTo { get; set; }

    /// <summary>Monaco MarkerSeverity: 8=Error, 4=Warning, 2=Info, 1=Hint.</summary>
    public int Severity { get; set; }

    public string Message { get; set; } = "";

    /// <summary>Код диагностики Roslyn, например CS1002.</summary>
    public string? Id { get; set; }
}
