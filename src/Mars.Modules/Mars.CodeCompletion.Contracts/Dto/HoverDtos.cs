namespace Mars.CodeCompletion.Contracts.Dto;

public class HoverResponseDto
{
    /// <summary>Markdown-содержимое подсказки.</summary>
    public string Content { get; set; } = "";

    public int OffsetFrom { get; set; }

    public int OffsetTo { get; set; }
}
