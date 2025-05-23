using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Nodes.FormEditor;

public partial class NodeEditContainer1
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    NodeFormEditorJsInterop js = default!;

    Node? _node = default!;
    [Parameter]
    public Node? Node
    {
        get => _node;
        set
        {
            if (value == _node) return;
            _node = value;
            NodeChanged.InvokeAsync(_node);
        }
    }
    [Parameter] public EventCallback<Node> NodeChanged { get; set; }
    [Parameter] public EventCallback<Node> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnBackdropCancel { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnClickEditConfigNode { get; set; } //TODO: Убрать это и все делать через _nodeEditorApi
    [Parameter] public EventCallback<Type> OnClickNewConfigNode { get; set; } // и это

    [Parameter] public bool DisableSaveOnBackdropClick { get; set; } = false;

    [CascadingParameter]
    INodeEditorApi _nodeEditorApi { get; set; } = default!;


    NodeFormEditor1 nodeFormEditor1 = default!;

    bool saveLoading = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        js = new(JS);

    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SubscribeOffcanvasHide();
        }
    }

    async void OpenOffcanvasEditor(bool show)
    {
        await js.ShowOffcanvas("node-editor-offcanvas", show);
    }

    public void StartEditNode(Node node)
    {
        Node = node.Copy();
        Node.Id = node.Id;
        OpenOffcanvasEditor(true);
    }

    public async Task FormSaveClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            saveLoading = true;
            StateHasChanged();

            await nodeFormEditor1.Form.OnEditSave();

            saveLoading = false;
            StateHasChanged();

            if (!nodeFormEditor1.EditForm.EditContext.Validate())
            {
                return;
            }
        }

        _ = OnSave.InvokeAsync(Node);
        OpenOffcanvasEditor(false);
    }

    async Task FormCloseClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            await nodeFormEditor1.Form.OnEditCancel();
        }
        OpenOffcanvasEditor(false);
        Node = null;
        await OnCancel.InvokeAsync();
    }

    async Task FormDeleteClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            await nodeFormEditor1.Form.OnEditDelete();
        }
        OpenOffcanvasEditor(false);
        //Node = null;
        await OnDelete.InvokeAsync(Node.Id);
    }

    Task OnValidSubmit()
    {
        return FormSaveClick();
    }

    //when close by shodow problem
    private DotNetObjectReference<NodeEditContainer1>? objRef;

    async void SubscribeOffcanvasHide()
    {
        objRef = DotNetObjectReference.Create(this);
        await js.SubscribeOffcanvasHide("#node-editor-offcanvas", objRef, "OnOffcanvasHide");
    }

    [JSInvokable]
    public async Task OnOffcanvasHide()
    {
        //Console.WriteLine("CW");
        //await FormSaveClick();

        if (DisableSaveOnBackdropClick)
        {
            await FormCloseClick();
        }
        else
        {
            await FormSaveClick();
        }

        await OnBackdropCancel.InvokeAsync();
    }

    public void Dispose()
    {
        objRef?.Dispose();
    }
}
