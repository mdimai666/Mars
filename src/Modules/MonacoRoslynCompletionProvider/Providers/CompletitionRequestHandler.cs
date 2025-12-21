using MonacoRoslynCompletionProvider.Dto;

namespace MonacoRoslynCompletionProvider.Providers;

public static class CompletitionRequestHandler
{
    static CompletionWorkspace workspace;

    public static async Task<TabCompletionResult[]> Handle(TabCompletionRequest tabCompletionRequest)
    {
        workspace ??= CompletionWorkspace.Create(tabCompletionRequest.Assemblies);

        //            tabCompletionRequest.Code = @$"
        //using System;

        //public class MyClass
        //{{

        //	public void MyMethod(int arg)
        //	{{
        //		{tabCompletionRequest.Code}		
        //	}}
        //}}
        //";

        var document = await workspace.CreateDocument(tabCompletionRequest.Code);
        return await document.GetTabCompletion(tabCompletionRequest.Position, CancellationToken.None);
    }

    public static async Task<HoverInfoResult> Handle(HoverInfoRequest hoverInfoRequest)
    {
        workspace ??= CompletionWorkspace.Create(hoverInfoRequest.Assemblies);
        var document = await workspace.CreateDocument(hoverInfoRequest.Code);
        return await document.GetHoverInformation(hoverInfoRequest.Position, CancellationToken.None);
    }

    public static async Task<CodeCheckResult[]> Handle(CodeCheckRequest codeCheckRequest)
    {
        workspace ??= CompletionWorkspace.Create(codeCheckRequest.Assemblies);
        var document = await workspace.CreateDocument(codeCheckRequest.Code);
        return await document.GetCodeCheckResults(CancellationToken.None);
    }

    public static async Task<SignatureHelpResult> Handle(SignatureHelpRequest signatureHelpRequest)
    {
        workspace ??= CompletionWorkspace.Create(signatureHelpRequest.Assemblies);
        var document = await workspace.CreateDocument(signatureHelpRequest.Code);
        return await document.GetSignatureHelp(signatureHelpRequest.Position, CancellationToken.None);
    }
}
