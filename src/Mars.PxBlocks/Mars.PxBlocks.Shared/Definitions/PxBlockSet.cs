using System.Collections;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Группа определений блоков по области («блоки миссии», «блоки датчиков») — аналог
/// пакета PXT. Блоки объявляются в конструкторе: <c>Add(PxMaster.Define("id").Message(...))</c>.
/// Реализация исполнения блоков будет регистрироваться отдельно, по TypeId.
/// </summary>
public abstract class PxBlockSet : IEnumerable<PxBlockDefinition>
{
    private readonly List<PxBlockDefinition> _definitions = [];

    public IReadOnlyList<PxBlockDefinition> Definitions => _definitions;

    protected void Add(PxBlockDefinition definition) => _definitions.Add(definition);

    public IEnumerator<PxBlockDefinition> GetEnumerator() => _definitions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
