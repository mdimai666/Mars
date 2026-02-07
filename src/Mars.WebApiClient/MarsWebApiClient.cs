using Flurl.Http;
using Mars.WebApiClient.Implements;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient;

public class MarsWebApiClient : IMarsWebApiClient
{
    public Func<IServiceProvider, IFlurlClient>? GetFlurlClientAction { get; set; }

    public IFlurlClient Client { get; }

    public IAccountServiceClient Account { get; }
    public IPostTypeServiceClient PostType { get; }
    public IFeedbackServiceClient Feedback { get; }
    public IPostServiceClient Post { get; }
    public IUserServiceClient User { get; }
    public IUserTypeServiceClient UserType { get; }
    public IPostCategoryTypeServiceClient PostCategoryType { get; }
    public IPostCategoryServiceClient PostCategory { get; }
    public IRoleServiceClient Role { get; }
    public INavMenuServiceClient NavMenu { get; }
    public IOptionServiceClient Option { get; }
    public IActServiceClient Act { get; }
    public IAppDebugServiceClient AppDebug { get; }
    public IMediaServiceClient Media { get; }
    public IPostJsonServiceClient PostJson { get; }
    public ISchedulerManagerClient Scheduler { get; }
    public IPluginServiceClient Plugin { get; }
    public ISystemServiceClient System { get; }
    public IPageRenderServiceClient PageRender { get; }
    public IFrontServiceClient Front { get; }
    public IAIServiceClient AITool { get; }

    public MarsWebApiClient(IServiceProvider serviceProvider, IFlurlClient flurlClient)
    {
        var targetClient = GetFlurlClientAction?.Invoke(serviceProvider) ?? flurlClient;
        Client = targetClient.OnError(BasicServiceClient.OnError);

        Account = new AccountServiceClient(serviceProvider, targetClient);
        PostType = new PostTypeServiceClient(serviceProvider, targetClient);
        Feedback = new FeedbackServiceClient(serviceProvider, targetClient);
        Post = new PostServiceClient(serviceProvider, targetClient);
        User = new UserServiceClient(serviceProvider, targetClient);
        UserType = new UserTypeServiceClient(serviceProvider, targetClient);
        PostCategoryType = new PostCategoryTypeServiceClient(serviceProvider, targetClient);
        PostCategory = new PostCategoryServiceClient(serviceProvider, targetClient);
        Role = new RoleServiceClient(serviceProvider, targetClient);
        NavMenu = new NavMenuServiceClient(serviceProvider, targetClient);
        Option = new OptionServiceClient(serviceProvider, targetClient);
        Act = new ActServiceClient(serviceProvider, targetClient);
        AppDebug = new AppDebugServiceClient(serviceProvider, targetClient);
        Media = new MediaServiceClient(serviceProvider, targetClient);
        PostJson = new PostJsonServiceClient(serviceProvider, targetClient);
        Scheduler = new SchedulerManagerClient(serviceProvider, targetClient);
        Plugin = new PluginServiceClient(serviceProvider, targetClient);
        System = new SystemServiceClient(serviceProvider, targetClient);
        PageRender = new PageRenderServiceClient(serviceProvider, targetClient);
        Front = new FrontServiceClient(serviceProvider, targetClient);
        AITool = new AIServiceClient(serviceProvider, targetClient);
    }
}
