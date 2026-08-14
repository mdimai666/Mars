namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Fluent-построитель <see cref="PxBlockDefinition"/>: создаётся через <see cref="PxMaster.Define"/>,
/// неявно приводится к определению. Классы-блоки в этом стиле не нужны; наследование
/// остаётся для блоков с динамической структурой (переопределение ToJson, мутаторы).
/// </summary>
public sealed class PxBlockBuilder
{
    private readonly PxBlockDefinition _definition;

    internal PxBlockBuilder(string typeId) => _definition = new PxBlockDefinition { TypeId = typeId };

    /// <summary>Блок-оператор с коннекторами предыдущий/следующий (поведение по умолчанию; метод — для читаемости).</summary>
    public PxBlockBuilder Statement()
    {
        _definition.OutputType = null;
        _definition.HasPrevious = true;
        _definition.HasNext = true;
        return this;
    }

    /// <summary>Блок-значение с выходом указанного типа.</summary>
    public PxBlockBuilder Output(string type)
    {
        _definition.OutputType = type;
        return this;
    }

    public PxBlockBuilder NoPrevious()
    {
        _definition.HasPrevious = false;
        return this;
    }

    public PxBlockBuilder NoNext()
    {
        _definition.HasNext = false;
        return this;
    }

    /// <summary>«Шапка» хат-блока — скруглённый верх событийных блоков (расширение px_hat_{hat}).</summary>
    public PxBlockBuilder Hat(string hat = "cap")
    {
        _definition.Hat = hat;
        return this;
    }

    public PxBlockBuilder Colour(string colour)
    {
        _definition.Colour = colour;
        return this;
    }

    public PxBlockBuilder Tooltip(string tooltip)
    {
        _definition.Tooltip = tooltip;
        return this;
    }

    /// <summary>
    /// Строка сообщения. Плейсхолдеры: именованные <c>{имя}</c> (порядок аргументов
    /// выводится из строки) либо позициянные <c>%1..%N</c> в порядке аргументов.
    /// </summary>
    public PxBlockBuilder Message(string message, params PxArg[] args)
    {
        _definition.Messages.Add(new PxMessageRow { Message = message, Args = [.. args] });
        return this;
    }

    public PxBlockBuilder Extensions(params string[] names)
    {
        _definition.Extensions.AddRange(names);
        return this;
    }

    /// <summary>Имя мутатора (Extensions.registerMutator) — для блоков с динамической структурой.</summary>
    public PxBlockBuilder Mutator(string name)
    {
        _definition.Mutator = name;
        return this;
    }

    public PxBlockDefinition Build() => _definition;

    public static implicit operator PxBlockDefinition(PxBlockBuilder builder) => builder.Build();
}
