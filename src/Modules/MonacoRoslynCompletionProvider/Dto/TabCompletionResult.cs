using MonacoRoslynCompletionProvider.Interfaces;

namespace MonacoRoslynCompletionProvider.Dto;

public class TabCompletionResult : IResponse
{
    public virtual string Suggestion { get; set; }
    public virtual string Description { get; set; }
    public int Kind { get; set; }
}
