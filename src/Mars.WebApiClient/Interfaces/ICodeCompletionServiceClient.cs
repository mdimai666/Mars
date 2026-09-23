using Mars.CodeCompletion.Contracts.Dto;

namespace Mars.WebApiClient.Interfaces;

public interface ICodeCompletionServiceClient
{
    /// <summary>null — фича выключена (FeatureGate дал 404).</summary>
    Task<CodeCompletionInfo?> GetInfo();

    Task<CompletionResponseDto> GetCompletions(string contextId, CodePositionRequest request);

    Task<HoverResponseDto?> GetHover(string contextId, CodePositionRequest request);

    Task<SignatureHelpResponseDto?> GetSignatureHelp(string contextId, CodePositionRequest request);

    Task<IReadOnlyList<DiagnosticDto>> GetDiagnostics(string contextId, CodePositionRequest request);
}
