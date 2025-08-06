namespace Mars.Shared.Interfaces;

public interface IAIToolAppService
{
    void Open(string text = "", string scenarioName = "");
    void Close();
}
