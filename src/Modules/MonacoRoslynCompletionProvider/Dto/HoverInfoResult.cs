using MonacoRoslynCompletionProvider.Interfaces;

namespace MonacoRoslynCompletionProvider.Dto;

public class HoverInfoResult : IResponse
{
    public virtual string Information { get; set; } = default!;

    public virtual int OffsetFrom { get; set; }

    public virtual int OffsetTo { get; set; }
}
