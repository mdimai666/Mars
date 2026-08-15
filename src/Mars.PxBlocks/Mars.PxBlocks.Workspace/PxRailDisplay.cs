namespace Mars.PxBlocks.Workspace;

/// <summary>Отображение рейки категорий в <see cref="PxBlocksEditor"/>.</summary>
public enum PxRailDisplay
{
    /// <summary>
    /// По умолчанию: полная рейка, при малой ширине редактора автоматически
    /// сворачивается в иконки (CSS container queries, без JS).
    /// </summary>
    Auto,

    /// <summary>Всегда полная рейка: иконки + имена категорий + строка поиска.</summary>
    Full,

    /// <summary>Всегда компактная рейка: только иконки; поиск — кнопка со всплывающим полем.</summary>
    Compact
}
