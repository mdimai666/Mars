using MonacoRoslynCompletionProvider.Interfaces;

namespace MonacoRoslynCompletionProvider.Dto;

public class TabCompletionRequest : IRequest
{
    public virtual string Code { get; set; }
    public virtual int Position { get; set; }
    public virtual string[] Assemblies { get; set; }

    public TabCompletionRequest()
    { }

    public TabCompletionRequest(string code, int position, string[] assemblies)
    {
        Code = code;
        Position = position;
        Assemblies = assemblies;
    }
}
