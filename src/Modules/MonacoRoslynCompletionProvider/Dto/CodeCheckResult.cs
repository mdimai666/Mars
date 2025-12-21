using MonacoRoslynCompletionProvider.Interfaces;

namespace MonacoRoslynCompletionProvider.Dto;

public class CodeCheckResult : IResponse
{
    public virtual string Id { get; set; }
    public virtual string Keyword { get; set; }
    public virtual string Message { get; set; }
    public virtual int OffsetFrom { get; set; }
    public virtual int OffsetTo { get; set; }
    public virtual CodeCheckSeverity Severity { get; set; }
    public virtual int SeverityNumeric { get; set; }
}
