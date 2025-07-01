using Flurl.Http;

namespace Mars.WebApiClient.Interfaces;

public interface IMarsWebApiClient
{
    Func<IServiceProvider, IFlurlClient>? GetFlurlClientAction { get; set; }
    IFlurlClient Client { get; }

    IAccountServiceClient Account { get; }
    IPostTypeServiceClient PostType { get; }
    IFeedbackServiceClient Feedback { get; }
    IPostServiceClient Post { get; }
    IUserServiceClient User { get; }
    IUserTypeServiceClient UserType { get; }
    IRoleServiceClient Role { get; }
    INavMenuServiceClient NavMenu { get; }
    IOptionServiceClient Option { get; }
    IActServiceClient Act { get; }
    IAppDebugServiceClient AppDebug { get; }
    IMediaServiceClient Media { get; }
    IPostJsonServiceClient PostJson { get; }
    ISchedulerManagerClient Scheduler { get; }
    IPluginServiceClient Plugin { get; }
    ISystemServiceClient System { get; }
    IPageRenderServiceClient PageRender { get; }
    IFrontServiceClient Front { get; }

}
