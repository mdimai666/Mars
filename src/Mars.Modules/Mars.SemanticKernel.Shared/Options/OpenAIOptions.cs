using System.ComponentModel.DataAnnotations;

namespace Mars.SemanticKernel.Shared.Options;

/// <summary>
/// OpenAI settings.
/// </summary>
public sealed class OpenAIOptions : ILLMOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultEndpoint = "https://api.openai.com/v1";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    public string Endpoint { get; set; } = DefaultEndpoint;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string? OrgId { get; set; } = string.Empty;
}
