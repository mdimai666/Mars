using MonacoRoslynCompletionProvider.Interfaces;

namespace MonacoRoslynCompletionProvider.Dto;

public class CodeCheckRequest : IRequest
{
    public virtual string Code { get; set; }
    public virtual string[] Assemblies { get; set; }

    public CodeCheckRequest()
    { }

    public CodeCheckRequest(string code, string[] assemblies)
    {
        Code = code;
        Assemblies = assemblies;
    }

}
