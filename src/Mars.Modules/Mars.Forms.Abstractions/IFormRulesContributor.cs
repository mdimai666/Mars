namespace Mars.Forms.Abstractions;

/// <summary>
/// Взнос провайдера в реестр правил формы: регистрирует правила, которым нужны данные владельца
/// (<c>unique</c> у поста, ограничения колонок у внешней таблицы) — встроенные правила общего слоя
/// чистые (см. <c>BuiltInFormRules</c>). Реализация регистрируется синглтоном в модуле провайдера
/// и применяется при создании <see cref="IFormRuleRegistry"/>, поэтому правила провайдера доступны
/// в любом хосте и в тестах, собирающих контейнер.
/// </summary>
public interface IFormRulesContributor
{
    void Register(IFormRuleRegistry registry);
}
