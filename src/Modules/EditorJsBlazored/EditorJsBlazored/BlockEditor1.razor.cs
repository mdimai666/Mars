using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static EditorJsBlazored.Blocks.BlockImage;

namespace EditorJsBlazored;

/// <summary>
/// block editor implementation of editor.js
/// <para/>
/// for render to html use <see cref="EditorTools.RenderToHtml(string)"/>
/// </summary>
public partial class BlockEditor1 : BlazorInteropComponent
{
    private IJSObjectReference _jsModule = default!;
    [Parameter] public bool AutoInitialize { get; set; } = true;

    [Parameter] public EditorJsConfig Config { get; set; } = new();
    [Parameter] public EventCallback ConfigChanged { get; set; }

    [Parameter] public EditorJsContent Content { get; set; } = default!;
    [Parameter] public EventCallback<EditorJsContent> ContentChanged { get; set; }
    [Parameter] public string? ContentJson { get; set; } = default!;
    [Parameter] public EventCallback<string> ContentJsonChanged { get; set; }

    [Parameter]
    public Func<Stream, string, Task<BlockImage.ImageFileData?>>? OnFileInput { get; set; }
    [Parameter]
    public Func<Task<BlockImage.ImageFileData?>>? OnImageFileRequest { get; set; }
    [Parameter]
    public Func<Task<BlockImage.ImageFileData?>>? OnAttachmentFileRequest { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Content is null && ContentJson is not null)
        {
            Content = EditorJsContent.FromJsonAutoConvertToRawBlockOnException(ContentJson, out var isReplaced);
            ContentJson = Content.ToJson(Config.PrettyJsonOutput);
            if (isReplaced)
            {
                await ContentChanged.InvokeAsync(Content);
                await ContentJsonChanged.InvokeAsync(ContentJson);
            }
        }
        else if (Content is not null)
        {
            ContentJson = Content.ToJson(Config.PrettyJsonOutput);
        }
        else
        {
            Console.Error.WriteLine("ContentError: Content must set or ContentJson");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/EditorJsBlazored/dist/EditorJsBlazored.iife.js");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// Sets the editors value via JS interop.
    /// </summary>
    /// <param name="data">Data coming from EditorJS</param>
    [JSInvokable]
    public async Task SetValue(string data)
    {
        Content = EditorJsContent.FromJson(data);
        ContentJson = Content.ToJson();

        await ContentChanged.InvokeAsync(Content);
        await ContentJsonChanged.InvokeAsync(ContentJson);

        StateHasChanged();
    }

    [JSInvokable]
    public void OnReady()
    {
        SetContent();
    }

    /// <summary>
    /// Initializes EditorJS component.
    /// </summary>
    protected override async Task InitializeClientComponent()
    {
        if (AutoInitialize)
        {
            await UpdateEditor();
        }
    }

    /// <summary>
    /// Updates the editor client component.
    /// </summary>
    public async Task UpdateEditor()
    {
        await JSRuntime.InvokeAsync<string>("editorJsHandler.initializeEditor", new object[] { ClientComponentID, DomElement, DotNetObjectReference.Create(this) });
    }

    public Task SetContent()
    {
        return CallClientMethod("render", Content);
    }

    [JSInvokable]
    public async Task<EditorJsUploadFileResult> UploadFileAction(byte[] buffer, string filename)
    {
        if (OnFileInput == null) throw new ArgumentNullException("Parameter OnFileInput not set");

        using var ms = new MemoryStream(buffer);

        var imageData = await OnFileInput(ms, filename);

        return new EditorJsUploadFileResult
        {
            Success = imageData == null ? 0 : 1,
            File = imageData,
        };
    }

    [JSInvokable]
    public async Task<ImageFileData?> OnClickSelectImageFileJSInvokable()
    {
        if (OnImageFileRequest == null)
        {
            throw new InvalidOperationException("OnImageFileRequest function is not set. Please set it to handle image file requests.");
        }

        var imageFile = await OnImageFileRequest();
        return imageFile;
    }

    [JSInvokable]
    public async Task<ImageFileData?> OnClickSelectAttachmentFileJSInvokable()
    {
        if (OnAttachmentFileRequest == null)
        {
            throw new InvalidOperationException("OnAttachmentFileRequest function is not set. Please set it to handle attachment file requests.");
        }

        var attachmentFile = await OnAttachmentFileRequest();
        return attachmentFile;
    }

    public void Clear()
    {
        Content = new();
        SetContent();
    }
}
