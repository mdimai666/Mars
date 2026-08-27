using Mars.Shared.Exceptions;

namespace Mars.Host.Shared.Services;

public interface IAIToolService
{
    /// <summary>
    /// Execute prompt to AI service as Tool execution settings(topK,topP).
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="AIServiceNotAvailableException" />
    /// <returns>LLM response as text</returns>
    Task<string> Prompt(string prompt, CancellationToken cancellationToken);
}
