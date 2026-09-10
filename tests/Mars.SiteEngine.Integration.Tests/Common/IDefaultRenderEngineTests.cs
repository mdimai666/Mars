namespace Mars.SiteEngine.Integration.Tests.Common;

public interface IDefaultRenderEngineTests
{
    Task Basic_IndexPage_Succeeds();
    Task Basic_SecondPage_Succeeds();
    Task Basic_Page404_ReturnsStatusCode404();
}
