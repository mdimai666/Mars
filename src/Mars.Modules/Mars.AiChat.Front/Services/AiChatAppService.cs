namespace Mars.AiChat.Front.Services;

public interface IAiChatModal
{
    bool IsVisible { get; }
    void Open();
    void Close();
    void Toggle();
}

public interface IAiChatAppService
{
    bool IsVisible { get; }
    void Open();
    void Close();
    void Toggle();
}

/// <summary>
/// Точка доступа к ИИ-чату из любого места админки.
/// Контейнер (AiChatTerminal) регистрирует себя через Setup при первом рендере.
/// </summary>
public class AiChatAppService : IAiChatAppService
{
    private static IAiChatModal? _modal;

    public static void Setup(IAiChatModal modal) => _modal = modal;

    public bool IsVisible => _modal?.IsVisible ?? false;

    public void Open() => _modal?.Open();

    public void Close() => _modal?.Close();

    public void Toggle() => _modal?.Toggle();
}
