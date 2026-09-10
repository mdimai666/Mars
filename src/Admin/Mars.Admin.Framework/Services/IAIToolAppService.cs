namespace Mars.Admin.Framework.Services;

public interface IAIToolAppService
{
    void Open(string text = "", string scenarioName = "");
    void Close();
}
