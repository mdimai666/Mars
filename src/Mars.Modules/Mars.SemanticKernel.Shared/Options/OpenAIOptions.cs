using System.ComponentModel.DataAnnotations;

namespace Mars.SemanticKernel.Shared.Options;

/// <summary>
/// OpenAI settings.
/// </summary>
public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string OrgId { get; set; } = string.Empty;
}
