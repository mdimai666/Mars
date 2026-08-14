namespace Mars.PxBlocks.Runtime.Execution;

public enum PxExecutionEventKind
{
    /// <summary>Интерпретатор вошёл в блок-оператор (id для подсветки в редакторе).</summary>
    BlockEntered,

    /// <summary>Оператор исполнен.</summary>
    BlockExited,

    /// <summary>Строка вывода (text_print); Text — содержимое.</summary>
    Output
}

/// <summary>
/// Событие исполнения — материал для подсветки бегущего блока, панели вывода,
/// отладчика. Стримится подписчику (PxRunOptions.OnEvent) по мере исполнения.
/// </summary>
public readonly record struct PxExecutionEvent(
    PxExecutionEventKind Kind,
    string? BlockId,
    string? Text);
