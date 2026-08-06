using System.ComponentModel.DataAnnotations;

namespace Mars.AiChat.Shared.Dto;

public class AiChatSendRequest
{
    [Required]
    public string Message { get; set; } = "";

    /// <summary>
    /// Контекст текущей страницы админки (URL), если открыта страница редактирования.
    /// </summary>
    public string? PageContext { get; set; }
}

public class AiChatCreateSessionRequest
{
    public string? Title { get; set; }
}
