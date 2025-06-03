using System.ComponentModel.DataAnnotations;

namespace Mars.SemanticKernel.Shared.Options;

/// <summary>
/// Ollama settings.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = "http://localhost:11434/";
}
