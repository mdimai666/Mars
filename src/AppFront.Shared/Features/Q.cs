using System.Security.Claims;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Features;
using AppFront.Shared.Mappings;
using AppFront.Shared.Models;
using Mars.Shared.Contracts.SSO;
using Mars.Shared.ViewModels;

namespace AppFront.Shared;

public static class Q
{
    public static string BackendUrl = "";
    public static Type Program = default!;
    public static string WorkDir = default!;

    public static Emitter Root = new();

    static UserFromClaims _user = new();
    public static UserFromClaims User => _user;

    static AppInitialViewModel _site = default!;
    public static AppInitialViewModel Site => _site;

    public static bool IsPrerender = false;// Program.IsPrerender;
#if DEBUG
    public static bool Dev = true;
#else
    public static bool Dev = false;
#endif

    public static void UpdateInitialSiteData(InitialSiteDataViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        _site = vm.ToModel();
        Root.Emit(Site.GetType(), vm);
    }

    public static void UpdateInitialSiteData(AppInitialViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        _site = vm;
        Root.Emit(Site.GetType(), vm);
    }

    public static void UpdateNotifyCount(int count)
    {
        Root.Emit("UpdateNotifyCount", count);
    }

    public static void UpdateUserByMarsClaims(ClaimsPrincipal marsClaims)
    {
        //Console.WriteLine(">UpdateUserByMarsClaims");
        _user = new UserFromClaims(marsClaims);
        Root.Emit(_user.GetType(), _user);
    }

    public static void UpdateUserByExternalClaims(ClaimsPrincipal externalClaims, SsoUserInfoResponse ssoUserInfo)
    {
        //Console.WriteLine(">UpdateUserByExternalClaims + internalUserId=" + ssoUserInfo.InternalId);
        _user = new UserFromClaims(ssoUserInfo); //Тут проблема что токен имеен другие клеймы
    }

    public static void UpdateUserByInitailVM(UserPrimaryInfo userPrimaryInfo, string? externalId)
    {
        //Console.WriteLine(">UpdateUserByInitailVM");
        _user = new UserFromClaims(userPrimaryInfo, externalId);
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

    public static List<string> Roles = ["Admin", "Manager", "Developer"];
    public static BackendHostingInfo HostingInfo { get; private set; } = default!;

    public static void SetupHostingInfo(BackendHostingInfo hostingInfo) => HostingInfo = hostingInfo;
}
