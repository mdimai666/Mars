namespace Mars.AiChat.Shared.Dto;

/// <summary>
/// Файл из медиатеки, приложенный к сообщению чата.
/// </summary>
public class AiChatAttachmentDto
{
    public Guid FileId { get; set; }
    public string Name { get; set; } = "";
    public string Ext { get; set; } = "";
    public ulong Size { get; set; }
    public bool IsImage { get; set; }

    /// <summary>
    /// Относительный URL файла (/upload/...).
    /// </summary>
    public string UrlRelative { get; set; } = "";
}
