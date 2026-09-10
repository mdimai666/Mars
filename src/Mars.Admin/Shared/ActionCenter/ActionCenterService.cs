namespace Mars.Admin.Shared.ActionCenter;

/// <summary>
/// Состояние палитры команд (открыта/закрыта). Мост между триггерами
/// (хоткеи в AdminLayout, кнопка в шапке) и самим компонентом палитры.
/// </summary>
public class ActionCenterService
{
    public event Action? StateChanged;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        StateChanged?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        StateChanged?.Invoke();
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }
}
