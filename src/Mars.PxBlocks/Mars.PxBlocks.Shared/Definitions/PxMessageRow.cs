namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Одна строка block-сообщения. Плейсхолдеры: именованные <c>{имя}</c> (порядок
/// аргументов выводится из строки) либо позициянные <c>%1..%N</c> в порядке объявления.
/// </summary>
public class PxMessageRow
{
    public string Message { get; set; } = "";
    public List<PxArg> Args { get; set; } = [];
}
