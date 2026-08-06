using System.ComponentModel.DataAnnotations;

namespace Mars.AiChat.Shared.Dto;

public class AiChatSendRequest
{
    [Required]
    public string Message { get; set; } = "";
}

public class AiChatCreateSessionRequest
{
    public string? Title { get; set; }
}
