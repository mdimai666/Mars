using System.Text;
using System.Text.Json;
using System.Web;
using Mars.Core.Features;
using Mars.Shared.Common;

namespace AppFront.Shared.Services;

public class StandartControllerClient<TEntity>
{
    //protected readonly HttpClient _httpClient;
    protected readonly QServer _client;

    public string _controllerName { get; set; }
    public string _basePath { get; set; }


    public StandartControllerClient(QServer client)
    {
        //_httpClient = client;
        _client = client;
        //_client.EnsureSuccessStatusCode = false;
        _basePath = "/api/";
        _controllerName = typeof(TEntity).Name;
    }

    public string GetQuery(BasicListQuery filter)
    {
        //string query = "filter="+GetQueryString(filter);
        //string query = GetQueryString(filter);

        string json = JsonSerializer.Serialize(filter);
        var bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(json));
        var str = Encoding.UTF8.GetString(bytes);
        //var compressed = RemoteLinq.Compress(str);

        string query = $"skip={filter.Skip}&take={filter.Take}&filter=" + Uri.EscapeDataString(str);
        //Console.WriteLine(JsonSerializer.Serialize(filter));

        return query;
    }

    public Task<TEntity?> Get(Guid id)
    {
        return _client.GET<TEntity>($"{_basePath}{_controllerName}/{id}");
    }

    public string QueryFilterAsQuery(BasicListQuery filter)
    {
        //string query = "filter="+GetQueryString(filter);
        //string query = GetQueryString(filter);

        string json = JsonSerializer.Serialize(filter);
        var bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(json));
        var str = Encoding.UTF8.GetString(bytes);
        //var compressed = RemoteLinq.Compress(str);

        string query = $"skip={filter.Skip}&take={filter.Take}&filter=" + Uri.EscapeDataString(str);

        return query;
    }

    public Task<PagingResult<TEntity>> ListTable(IBasicListQuery filter, string extraQuery = "")
    {
        return _client.Post<PagingResult<TEntity>>($"{_basePath}{_controllerName}/ListTable?{extraQuery}", filter)!;
    }

    public Task<UserActionResult<TEntity?>> Add(TEntity entity)
    {
        try
        {
            return _client.Post<UserActionResult<TEntity?>>($"{_basePath}{_controllerName}", entity!)!;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new UserActionResult<TEntity?>
            {
                Ok = false,
                Message = ex.Message,
            });
        }
    }

    public Task<UserActionResult<TEntity>> Update(TEntity entity)
    {
        try
        {
            return _client.PUT<UserActionResult<TEntity>>($"{_basePath}{_controllerName}", entity!)!;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new UserActionResult<TEntity>
            {
                Ok = false,
                Message = ex.Message,
            });
        }

    }

    public Task<UserActionResult> Delete(Guid id)
    {
        try
        {
            return _client.DELETE<UserActionResult>($"{_basePath}{_controllerName}/{id}")!;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new UserActionResult
            {
                Ok = false,
                Message = ex.Message
            });
        }
    }

    //public async Task<TEntity> Patch(Guid id, JsonPatchDocument<TEntity> patch)
    //{
    //    return await _client.Patch<TEntity>($"{_basePath}{_controllerName}/{id}", patch);
    //}

    public string GetQueryString(object obj)
    {
        //return Uri.EscapeDataString(JsonSerializer.Serialize(obj));

        var properties = from p in obj.GetType().GetProperties()
                         where p.GetValue(obj, null) != null
                         select p.Name + "=" + HttpUtility.UrlEncode(p?.GetValue(obj, null)?.ToString());

        List<string> arr = new List<string>();

        foreach (var p in obj.GetType().GetProperties())
        {
            var val = p.GetValue(obj, null);
            if (val != null)
            {
                if (p.PropertyType is object)
                {
                    //arr.Add(p.Name + "=" + HttpUtility.UrlEncode(JsonSerializer.Serialize(val)));
                    arr.Add(p.Name + "=" + JsonSerializer.Serialize(val));
                }
                else
                {
                    //arr.Add(p.Name + "=" + HttpUtility.UrlEncode(val.ToString()));
                    arr.Add(p.Name + "=" + val.ToString());
                }
            }
        }

        //return String.Join("&", properties.ToArray());
        return string.Join("&", arr);
    }
}
