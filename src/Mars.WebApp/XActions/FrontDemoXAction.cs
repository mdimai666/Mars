namespace Mars.XActions;

/// <summary>
/// Отладочное фронтовое действие (<see cref="Mars.Shared.Contracts.XActions.XActionType.FrontAction"/>):
/// метаданные регистрируются на хосте, исполнение — на клиенте через раннер
/// (см. AppAdmin.Shared.FrontDemoActionRunner). Сами продуктовые фронтовые действия
/// (тема, язык) будут регистрироваться по этому образцу.
/// </summary>
public static class FrontDemoXAction
{
    public const string CommandId = "Mars.Debug.FrontDemo";
    public const string Label = "Фронтовое демо-действие";
}
