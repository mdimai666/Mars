namespace Mars.PxBlocks.Runtime.Values;

/// <summary>
/// Значение рантайма PxBlocks — зеркало типов стыковки (PxTypeRegistry в Shared):
/// Number/Boolean/String/Object/List + Null. Приведения и истинность — в духе
/// целевой семантики Blockly (JS-подобной), но без неявных сюрпризов.
/// </summary>
public abstract record PxValue
{
    /// <summary>Имя типа значения (соответствует PxType.Name реестра стыковок).</summary>
    public abstract string TypeName { get; }

    /// <summary>Истинность как в Blockly: 0, NaN, пустая строка, false и null — ложь.</summary>
    public virtual bool IsTruthy() => true;

    public virtual double ToNumber() => double.NaN;

    public virtual string ToText() => "";

    /// <summary>Сложение: числа складываются; если хотя бы один операнд — строка, это конкатенация.</summary>
    public virtual PxValue Add(PxValue other)
    {
        if (this is PxStringValue || other is PxStringValue)
            return new PxStringValue(ToText() + other.ToText());
        return new PxNumberValue(ToNumber() + other.ToNumber());
    }

    public sealed override string ToString() => ToText();
}
