using BlazorMonaco.Editor;

namespace Mars.CodeCompletion.Front.Services;

public interface ICodeCompletionAttacher
{
    /// <summary>
    /// Подключает серверный IntelliSense (completion/hover/signature/диагностика) к редактору.
    /// Возвращает null, если фича выключена на сервере; dispose результата отключает контекст.
    /// Вызывать после инициализации редактора (когда доступна его model).
    /// </summary>
    Task<IAsyncDisposable?> AttachAsync(StandaloneCodeEditor editor, string contextId);
}
