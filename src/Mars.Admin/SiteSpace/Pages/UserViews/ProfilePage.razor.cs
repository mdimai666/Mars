using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.MetaFields;
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

    List<(MetaFieldEditModel Field, string Text)> MetaRows
        => [.. (_model?.MetaValues ?? [])
            .Where(v => v.MetaField is not null && !v.MetaField.Hidden)
            .GroupBy(v => v.MetaField.Key)
            .Select(g =>
            {
                var text = string.Join(", ", g.Select(FormatMeta).Where(s => s != "—"));
                return (g.First().MetaField, Text: text.Length > 0 ? text : "—");
            })
            .OrderBy(t => t.Item1.Order)];

    static string FormatMeta(MetaValueEditModel v)
    {
        var f = v.MetaField;
        var s = f.Type switch
        {
            MetaFieldType.Bool => v.Bool ? "да" : "нет",
            MetaFieldType.Int => v.Int.ToString(),
            MetaFieldType.Long => v.Long.ToString(),
            MetaFieldType.Float => v.Float.ToString("0.##"),
            MetaFieldType.Decimal => v.Decimal.ToString("0.##"),
            MetaFieldType.DateTime => v.DateTime?.ToString("d") ?? "",
            MetaFieldType.Select => f.Variants.FirstOrDefault(x => x.Id == v.VariantId)?.Title ?? v.StringShort,
            MetaFieldType.SelectMany => string.Join(", ", v.VariantsIds
                .Select(id => f.Variants.FirstOrDefault(x => x.Id == id)?.Title)
                .Where(x => !string.IsNullOrEmpty(x))),
            MetaFieldType.Text => string.IsNullOrWhiteSpace(v.StringText) ? v.StringShort : v.StringText,
            _ => string.IsNullOrWhiteSpace(v.StringShort) ? v.StringText : v.StringShort,
        };
        return string.IsNullOrWhiteSpace(s) ? "—" : s;
    }
}
