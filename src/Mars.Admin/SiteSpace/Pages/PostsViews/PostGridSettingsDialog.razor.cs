using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.PostsViews;

public partial class PostGridSettingsDialog
{
    [Parameter] public PostGridSettingsDialogContent Content { get; set; } = default!;
    [CascadingParameter] public IDialogInstance Dialog { get; set; } = default!;

    PostTypeGridSettings? _draft;

    protected override void OnInitialized() => _draft = Content.Settings;

    Task CancelAsync() => Dialog.CancelAsync();

    // результат — всегда ненулевой wrapper: null-настройки означают «сброс к стандартным»,
    // и через DialogResult.Value их нельзя отличить от отмены
    Task SaveAsync() => Dialog.CloseAsync(new PostGridSettingsDialogResult(_draft));
}

public class PostGridSettingsDialogContent
{
    public PostTypeGridSettings? Settings { get; init; }
    public IReadOnlyCollection<string> EnabledFeatures { get; init; } = [];
    public IReadOnlyCollection<MetaFieldDetailResponse>? MetaFields { get; init; }
}

public record PostGridSettingsDialogResult(PostTypeGridSettings? Settings);
