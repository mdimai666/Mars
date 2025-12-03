namespace Mars.AppFrontEngines.Integration.Tests.Common;

public interface IDefaultRenderEngineTests
{
    Task Basic_IndexPage_ShouldOk();
    Task Basic_SecondPage_ShouldOk();
    Task Basic_Page404_ShouldStatusCode404();
}
