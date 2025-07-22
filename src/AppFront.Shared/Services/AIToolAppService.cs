using Mars.Shared.Interfaces;

namespace AppFront.Shared.Services;

public class AIToolAppService : IAIToolAppService
{
    static IAIToolModal? _aIToolModal;

    public void Close() => _aIToolModal?.Close();

    public void Open(string text = "", string scenarioName = "") => _aIToolModal?.Open(text, scenarioName);

    public static void Setup(IAIToolModal modal)
    {
        _aIToolModal = modal;
    }
}

public interface IAIToolModal
{
    void Open(string text = "", string scenarioName = "");
    void Close();
    bool IsVisible { get; }
}
