using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Core.Features;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Common;
using Mars.Shared.Contracts.Search;
using Mars.Shared.Contracts.Systems;
using Mars.Shared.ViewModels;
using Flurl.Http;
using Microsoft.JSInterop;

namespace AppFront.Shared.Services;

public class ViewModelService : IViewModelService
{
    protected readonly IFlurlClient _client;
    protected string _controllerName { get; set; }
    protected string _basePath { get; set; }

    protected readonly IJSRuntime jsRuntime;

    public ViewModelService(IFlurlClient flurlClient, IJSRuntime JSRuntime)
    {
        _basePath = "/vm/";
        _controllerName = "ViewModel";

        _client = flurlClient;
        jsRuntime = JSRuntime;
    }

    async Task<T> Get<T>(string query = "") where T : class
    {
        var tname = typeof(T).Name;
        var result = await _client.Request($"{_basePath}{_controllerName}/{tname}?{query}").GetJsonAsync<T>();
        return result ?? throw new NotFoundException();
    }

    public async Task<bool> TryUpdateInitialSiteData(bool forceRemote = false, bool devAdminPageData = false)
    {
        if (Q.IsPrerender) return true;

        InitialSiteDataViewModel? vm = null;

        if (!forceRemote)
        {
            vm = await GetLocalInitialSiteDataViewModel();
            //Console.WriteLine("_vm:" + _vm.NavMenus.Count);
        }
        vm ??= await InitialSiteDataViewModel(devAdminPageData: devAdminPageData);

        if (vm == null)
        {
            Console.Error.WriteLine("InitialSiteDataViewModel is null");
            return false;
        }
        else
        {
            Q.UpdateInitialSiteData(vm);
        }
        return true;
    }

    public async Task<InitialSiteDataViewModel> InitialSiteDataViewModel(bool devAdminPageData = false)
    {
        return await Get<InitialSiteDataViewModel>("devAdminPageData=" + devAdminPageData.ToString().ToLower());
    }

    SemaphoreSlim __DevAdminExtraViewModel = new(1);
    static DevAdminExtraViewModel DevViewModel { get; set; } = default!;

    public async Task<DevAdminExtraViewModel> DevAdminExtraViewModel(bool force = false)
    {
        if (DevViewModel is not null && !force) return DevViewModel;

        try
        {
            await __DevAdminExtraViewModel.WaitAsync();

            if (DevViewModel is not null && !force) goto End;

            DevViewModel = await Get<DevAdminExtraViewModel>();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            __DevAdminExtraViewModel.Release();
        }
    End:
        return DevViewModel;
    }

    public async Task<EditUserViewModel> EditUserViewModel(Guid id)
    {
        return await Get<EditUserViewModel>($"id={id}");
    }

    public string SystemExportSettingsUrl()
    {
        string url = _client.BaseUrl + "vm/ViewModel/SystemExportSettings";
        return url;
    }

    public async Task<UserActionResult> SystemImportSettings(SystemImportSettingsFile_v1Request val)
    {
        var result = await _client.Request($"{_basePath}{_controllerName}/SystemImportSettings")
            .PostJsonAsync(val)
            .ReceiveJson<UserActionResult>();
        return result;
    }

    public async Task<StatisticPageViewModel> StatisticPageViewModel()
    {
        return await Get<StatisticPageViewModel>();
    }

    public async Task<List<SearchFoundElementResponse>> GlobalSearch(string text, int maxCount = 20)
    {
        return await _client.Request($"{_basePath}{_controllerName}/GlobalSearch")
            .AppendQueryParam(new { text , maxCount })
            .GetJsonAsync<List<SearchFoundElementResponse>>();
    }

    SemaphoreSlim initialSiteDataIsLoad = new(1);

    public async Task<InitialSiteDataViewModel> GetLocalInitialSiteDataViewModel(bool force = false)
    {
        InitialSiteDataViewModel? vm;

        if (Q._site is not null && !force) return Q._site;

        try
        {
            await initialSiteDataIsLoad.WaitAsync();

            if (Q._site is not null && !force) goto End;

            JsonSerializerOptions opt = new()
            {
                PropertyNameCaseInsensitive = true,
            };

            var vm1 = await jsRuntime.InvokeAsync<JsonElement>("InitialSiteDataViewModel");
            if (vm1.ValueKind == JsonValueKind.Object)
            {
                vm = JsonSerializer.Deserialize<InitialSiteDataViewModel>(vm1, opt);
            }
            else
            {
                string vm_json = vm1.ToString();

                if (vm_json[0] != '{')
                {
                    vm_json = TextZip.UnzipFromBase64(vm_json);
                }
                vm = System.Text.Json.JsonSerializer.Deserialize<InitialSiteDataViewModel>(vm_json, opt)!;
            }
            if (vm is null)
            {
                throw new ArgumentNullException("InitialSiteDataViewModel");
            }

            Q._site = vm;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            initialSiteDataIsLoad.Release();
        }
    End:
        return Q._site;
    }
}
