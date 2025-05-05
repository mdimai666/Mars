using System.Web;
using Mars.Core.Features;

namespace AppFront.Shared.Services;

public class MinimalClientService
{
    protected string _controllerName = default!;
    protected string _basePath;

    protected readonly QServer _client;
    protected readonly IServiceProvider serviceProvider;

    public MinimalClientService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        _basePath = "/api/";
        _client = serviceProvider.GetRequiredService<QServer>();
    }

    public static string GetQueryString(object obj)
    {
        var properties = from p in obj.GetType().GetProperties()
                         where p.GetValue(obj, null) != null
                         select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

        return String.Join("&", properties.ToArray());
    }
}
