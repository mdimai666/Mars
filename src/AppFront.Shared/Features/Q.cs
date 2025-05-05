using System.Security.Claims;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Features;
using Mars.Shared.ViewModels;
using Microsoft.Extensions.Logging;

namespace AppFront.Shared;

public class Q
{
    public static string BackendUrl = "";
    public static Type Program = default!;
    public static string WorkDir = default!;

    public static Emitter Root = new Emitter();

    public static string AuthToken = default!;

    static UserFromClaims _user = new();

    public static UserFromClaims User { get => _user; }


    public static InitialSiteDataViewModel _site = default!;
    public static InitialSiteDataViewModel Site => _site;

    static IServiceProvider _serviceProvider = default!;

    public static bool IsPrerender = false;// Program.IsPrerender;

#if DEBUG
    public static bool Dev = true;
#else
    public static bool Dev = false; 
#endif

    public static void AddSrvProv(IServiceProvider sp)
    {
        _serviceProvider = sp;
        //_logger = sp.GetService<ILogger>();//not work
    }

    public static void LogWarn(string text)
    {
        //_logger.LogWarning(text);//not work
    }

    public static void UpdateInitialSiteData(InitialSiteDataViewModel vm)
    {
        if (vm == null)
        {
            Console.WriteLine("InitialSiteDataViewModel is null");
            return;
        }
        _site = vm;
        Root.Emit(Site.GetType(), vm);

    }

    //public static void UpdateProfile(Profile profile)
    //{
    //    if (profile == null)
    //    {
    //        _profile = _anonymous;
    //        Root.Emit(_profile.GetType(), _profile);
    //        return;
    //    }

    //    // Console.WriteLine("UpdateProfile");
    //    _profile = profile;
    //    Root.Emit(_profile.GetType(), _profile);
    //}

    public static void UpdateNotifyCount(int count)
    {
        Root.Emit("UpdateNotifyCount", count);
    }

    public static void UpdateClaimUser(ClaimsPrincipal claimsPrincipal)
    {
        _user = new UserFromClaims(claimsPrincipal);
        Root.Emit(_user.GetType(), _user);
    }
    public static void LogoutUser()
    {
        _user = new UserFromClaims(new ClaimsPrincipal());
        Root.Emit(_user.GetType(), _user);
    }

    public static string ServerUrlJoin(string path)
    {
        if (String.IsNullOrEmpty(path)) return path;

        string domain = $"{BackendUrl}/";

        if (path.Contains("://"))
        {
            path = path.Replace(domain.TrimEnd('/'), "");
        }

        return domain + path.TrimStart('/');
    }

    public static string ClientUrlJoin(string path)
    {
        string domain = $"{Q.Site.SysOptions.SiteUrl.TrimEnd('/')}/";

        if (path.Contains("://"))
        {
            path = path.Replace(domain.TrimEnd('/'), "");
        }

        return domain + path.TrimStart('/');
    }

    public static List<string> Roles = new() { "Admin", "Manager", "Developer" };

    //public List<PostType> PostTypes { get; set; }
    //public List<PostCategory> PostCategories { get; set; }

    public static BackendHostingInfo HostingInfo { get; private set; } = default!;

    public static void SetupHostingInfo(BackendHostingInfo hostingInfo) => HostingInfo = hostingInfo;
}

