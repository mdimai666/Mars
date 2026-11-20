using System.ComponentModel;
using Mars.AiChat.Contracts.SignalR;
using Mars.AiChat.Host.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента для обновления данных сайта в открытой админке.
/// Экземпляр создаётся на каждый запуск с chatId: команда уходит в группу чата,
/// терминал в браузере перетягивает InitialSiteDataViewModel (как AfterSave у EditPostType).
/// </summary>
public class MarsSiteDataTools
{
    private readonly IHubContext<AiChatHub> _hub;
    private readonly Guid _chatId;

    public MarsSiteDataTools(IHubContext<AiChatHub> hub, Guid chatId)
    {
        _hub = hub;
        _chatId = chatId;
    }

    [Description("Обновить InitialSiteData в открытой админке: боковое меню (сайдбар), настройки сайта, типы записей. " +
                 "Вызывай после изменения меню или настроек серверными инструментами, чтобы пользователь увидел " +
                 "изменения без перезагрузки страницы. Работает, когда админка открыта в браузере.")]
    public async Task<string> RefreshInitialSiteData()
    {
        try
        {
            await _hub.Clients.Group(AiChatHub.GroupName(_chatId))
                .SendCoreAsync(AiChatHubEvents.RefreshSiteData, [_chatId]);

            return "Команда обновления InitialSiteData отправлена в админку: сайдбар и данные сайта обновятся без перезагрузки.";
        }
        catch (Exception ex)
        {
            return "Ошибка: " + ex.GetBaseException().Message;
        }
    }
}