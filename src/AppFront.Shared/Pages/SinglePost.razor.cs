using AppFront.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppFront.Shared.Pages;

public partial class SinglePost
{
    string? _oldId = null;

    string? _id = null;
    [Parameter]
    public string IDorSLUG
    {
        get => _id!;
        set
        {
            _oldId = _id;
            _id = value;
            if (string.IsNullOrEmpty(POSTTYPE) == false)
            {
                Load();
            }

        }
    }
    [Parameter] public string POSTTYPE { get; set; } = default!;

    bool MauiReplaceUrl => OnePage.MauiReplaceUrl;

    //public Post Post { get; set; } = null;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] IMarsWebApiClient _client { get; set; } = default!;

    public string? CurrentPageTitle { get; set; }

    RenderActionResult<PostRenderResponse>? res = null;

    bool Busy = false;
    string? _error;

    [Inject] IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] ViewModelService vms { get; set; } = default!;

    PostRenderResponse postRender => res?.Data!;

    async void Load(bool force = false, bool hot = false)
    {
        if (string.IsNullOrEmpty(IDorSLUG) || Busy) return;
        _error = null;

        var inLocal = Q.Site.LocalPages.FirstOrDefault(s => !s.NotFound && (s.Data.DataId.ToString() == IDorSLUG || s.Data.PostSlug == IDorSLUG));

        if (inLocal is not null && !force)
        {
            res = inLocal;
            if (MauiReplaceUrl && res.Data is not null)
            {
                res.Data.Html = OnePage.ReplaceRelativeUrls(res.Data.Html);
            }
            CurrentPageTitle = inLocal.Data.Title;

            _oldId = inLocal.Data.PostSlug;
            StateHasChanged();
            return;
        }

        if (Q.IsPrerenderProcess)
        {
            Busy = true;
            return;
        }
        ;

        if (!hot) Busy = true;
        StateHasChanged();

        try
        {
            if (postRender is null || _oldId != _id || force)
            {
                res = await _client.PageRender.RenderPost(POSTTYPE, IDorSLUG);
                if (MauiReplaceUrl && res.Data is not null)
                {
                    res.Data.Html = OnePage.ReplaceRelativeUrls(res.Data.Html);
                }
                CurrentPageTitle = postRender?.Title;
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }

        if (hot)
        {
            _ = JSRuntime.InvokeVoidAsync("triggerHotCheckAni");
        }

        Busy = false;
        StateHasChanged();
    }
}
