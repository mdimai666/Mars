using Microsoft.CodeAnalysis;

namespace Mars.CodeCompletion.Host.Abstractions;

/// <summary>
/// Провайдер объявляет контекст кода для своего редактора: global-объект скрипта,
/// импорты и видимые сборки. Регистрируется в DI, менеджер воркспейсов подбирает его по <see cref="ContextId"/>.
/// </summary>
public interface ICodeContextProvider
{
    /// <summary>Slug контекста, используется в URL: api/CodeCompletion/{ContextId}/...</summary>
    string ContextId { get; }

    /// <summary>Тип глобального объекта скрипта (для FunctionNode — ScriptExecuteContext); null для обычного файла.</summary>
    Type? HostObjectType => null;

    IReadOnlyList<string> Imports { get; }

    /// <summary>
    /// Дополнительные ссылки на сборки, которые должно видеть редактируемый код.
    /// Доверенные платформенные сборки (TrustedPlatformAssemblies) менеджер добавляет сам.
    /// </summary>
    IReadOnlyCollection<MetadataReference> GetMetadataReferences();

    /// <summary>true — SourceCodeKind.Script: top-level statements, return вне метода, глобальный объект.</summary>
    bool IsScript => true;
}
