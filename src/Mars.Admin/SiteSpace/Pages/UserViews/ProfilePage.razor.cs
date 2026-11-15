using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.UserViews;

public partial class ProfilePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    ProfileEditModel? _model;

    protected override async Task OnInitializedAsync()
    {
        var vm = await client.Account.GetProfileEditModel();
        _model = ProfileEditModel.FromViewModel(vm);
    }

    static string Value(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s;

    bool HasMeta => _model is not null && MetaValuesDefList.HasVisibleValues(_model.MetaValues);
}
