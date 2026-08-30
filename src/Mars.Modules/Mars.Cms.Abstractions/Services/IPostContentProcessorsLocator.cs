using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Abstractions.Services;

public interface IPostContentProcessorsLocator
{
    IReadOnlyCollection<string> ListKeys(string[]? tags = null);

    /// <summary>
    /// GetProvider
    /// </summary>
    /// <param name="postContentType">
    /// Ключ редактора контента (<see cref="MetaFieldEditorCatalog"/>) —
    /// редактор поля контента типа (<c>Options.editor</c> поля <c>content</c>).
    /// </param>
    /// <returns></returns>
    IPostContentProcessor? GetProvider(string postContentType, IServiceProvider serviceProvider);
}
