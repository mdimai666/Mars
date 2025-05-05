namespace Mars.Core.Constants;

/// <summary>
///     Основные константы инфраструктуры
/// </summary>
public class HttpConstants
{
    /// <summary>
    ///     HTTP-код отсутствия прав у пользователя
    /// </summary>
    public const int ForbiddenCode403 = 403;

    /// <summary>
    ///     HTTP-код обрабатываемой бизнес-ошибки
    ///     для визуализации сообщений пользователю
    /// </summary>
    public const int UserActionErrorCode466 = 466;

    /// <summary>
    ///     HTTP-код создания запроса на подтверждение действия
    ///     для визуализации окна подтверждения
    /// </summary>
    public const int ConfirmedCode265 = 265;

    public const int BadRequest400 = 400;
}
