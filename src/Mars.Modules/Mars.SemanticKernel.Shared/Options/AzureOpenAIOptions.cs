using System.ComponentModel.DataAnnotations;

namespace Mars.SemanticKernel.Shared.Options;

/// <summary>
/// Azure OpenAI settings.
/// </summary>
public sealed class AzureOpenAIOptions : ILLMOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required]
    public string ChatDeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
