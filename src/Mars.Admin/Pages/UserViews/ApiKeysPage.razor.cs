using Mars.Admin.Framework;
using Mars.Identity.Contracts.ApiKeys;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using IMessageService = Mars.Admin.Framework.Interfaces.IMessageService;

namespace Mars.Admin.Pages.UserViews;

public partial class ApiKeysPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;
    [Inject] AdminJs adminJs { get; set; } = default!;

    FluentDataGrid<ApiKeySummaryResponse> table = default!;
    GridItemsProvider<ApiKeySummaryResponse> dataProvider = default!;
    List<ApiKeySummaryResponse> keys = [];

    static DateTime Week => DateTime.Today.AddDays(7);
    static DateTime Month => DateTime.Today.AddMonths(1);
    static DateTime Quarter => DateTime.Today.AddMonths(3);
    static DateTime Year => DateTime.Today.AddYears(1);

    bool createVisible;
    string newKeyName = "";
    DateTime? newKeyExpires;

    bool createdKeyVisible;
    CreatedApiKeyResponse? createdKey;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<ApiKeySummaryResponse>(
            async req =>
            {
                keys = (await client.ApiKey.List()).ToList();
                return GridItemsProviderResult.From(keys, keys.Count);
            });
    }

    void OnClickCreate()
    {
        newKeyName = "";
        newKeyExpires = Quarter;
        createVisible = true;
    }

    void SetExpires(DateTime value) => newKeyExpires = value;

    bool IsChipActive(DateTime value) => newKeyExpires?.Date == value.Date;

    async Task CreateKey()
    {
        if (string.IsNullOrWhiteSpace(newKeyName))
        {
            _ = messageService.Error("Укажите название ключа");
            return;
        }

        var result = await client.ApiKey.Create(new CreateApiKeyRequest
        {
            Name = newKeyName.Trim(),
            ExpiresAt = newKeyExpires is null ? null : new DateTimeOffset(newKeyExpires.Value),
        });

        if (!result.Ok)
        {
            _ = messageService.Error(result.Message);
            return;
        }

        createVisible = false;
        createdKey = result.Data;
        createdKeyVisible = true;

        await table.RefreshDataAsync();
    }

    async Task Revoke(Guid id)
    {
        var result = await client.ApiKey.Revoke(id);

        if (result.Ok)
        {
            await table.RefreshDataAsync();
            _ = messageService.Success(result.Message);
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }

    async Task CopyCreatedKey()
    {
        if (createdKey is null) return;

        await adminJs.CopyToClipboard(createdKey.Key);
        _ = messageService.Success("Скопировано");
    }
}
