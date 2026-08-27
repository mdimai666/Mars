namespace Mars.Contracts.Interfaces;

public interface IAIToolAppService
{
    void Open(string text = "", string scenarioName = "");
    void Close();
}
