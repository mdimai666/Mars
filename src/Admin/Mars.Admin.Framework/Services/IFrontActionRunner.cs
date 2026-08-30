using Mars.XActions.Contracts;

namespace Mars.Admin.Framework.Services;

/// <summary>
/// Клиентский исполнитель фронтового действия (<see cref="XActionType.FrontAction"/>).
/// Метаданные команды регистрируются на хосте как обычно (система видит её в реестре),
/// но исполнение происходит здесь, на клиенте. Реестр собирается из всех раннеров в DI.
/// </summary>
public interface IFrontActionRunner
{
    /// <summary>
    /// Id XAction, которую исполняет этот раннер.
    /// </summary>
    string ActionId { get; }

    Task<XActResult> ExecuteAsync(IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken);
}
