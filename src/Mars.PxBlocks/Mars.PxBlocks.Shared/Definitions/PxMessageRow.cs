namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>Одна строка block-сообщения: текст с плейсхолдерами %1..%N и аргументы к ним.</summary>
public class PxMessageRow
{
    public string Message { get; set; } = "";
    public List<PxArg> Args { get; set; } = [];
}
