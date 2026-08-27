using Mars.Admin.Framework.Services;
using Mars.Contracts.XActions;

namespace Mars.Admin.Shared;

/// <summary>
/// DEBUG-раннер демонстрационного фронтового действия: исполняется на клиенте
/// и показывает тост. Образец для продуктовых фронтовых действий
/// (переключение темы, языка и т.п.). Id должен совпадать с
/// FrontDemoXAction.CommandId в Mars.WebApp.
/// </summary>
public class FrontDemoActionRunner : IFrontActionRunner
{
    public string ActionId => "Mars.Debug.FrontDemo";

    public Task<XActResult> ExecuteAsync(IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
        => Task.FromResult(XActResult.ToastSuccess("Фронтовое действие выполнено на клиенте"));
}
