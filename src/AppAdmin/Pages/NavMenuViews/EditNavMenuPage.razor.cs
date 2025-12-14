using System.Reflection.Metadata;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Shared.Contracts.Roles;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AppAdmin.Pages.NavMenuViews;

public partial class EditNavMenuPage
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Parameter] public Guid ID { get; set; }
    StandartEditContainer<NavMenu> f = default!;

    NavMenuItem? _selMenu => _selNode?.Node;

    DTreeNode<NavMenuItem>? _selNode;
    IReadOnlyCollection<RoleSummaryResponse> _availRoles = [];

    protected override void OnInitialized()
    {
        initExport();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Task.Run(async () =>
            {
                _availRoles = (await client.Role.List(new())).Items;
            });
        }
    }

    void OnClickItem(DTreeNode<NavMenuItem> node)
    {
        if (_selNode == node) _selNode = null;
        else _selNode = node;
    }

    void OnDeleteMenuItem(NavMenuItem item)
    {
        //if (_selMenu == item) _selMenu = null;
        if (_selMenu == item) _selNode = null;

        TheRecurseDelete(item);
        f.Model.MenuItems = f.Model.MenuItems.Except([item]);
    }

    void TheRecurseDelete(NavMenuItem item)
    {
        foreach (var h in item.GetItems(f.Model))
        {
            TheRecurseDelete(h);
        }
    }

    void DropdownMenu_Copy(NavMenuItem item)
    {
        var copy = NavMenuItem.Copy(item);
        var list = f.Model.MenuItems.ToList();
        int index = list.IndexOf(item);
        list.Insert(index, copy);
        f.Model.MenuItems = list;
    }

    void AddNewMenuItem()
    {
        var list = f.Model.MenuItems.ToList();
        list.Add(new()
        {
            Title = $"Меню ({list.Count})"
        });
        f.Model.MenuItems = list;
    }

    //void TreeOnDragEnd(TreeEventArgs<NavMenuItem> e)
    //{
    //    //Console.WriteLine("TreeOnDragEnd");
    //    //Console.WriteLine($">> e.TargetNode = {e.TargetNode.DataItem.Title}");
    //    //Console.WriteLine($">> e.Node = {e.Node.DataItem.Title}");
    //}
    //void TreeOnDrop(TreeEventArgs<NavMenuItem> e)
    //{
    //    //Console.WriteLine("TreeOnDrop");
    //    Console.WriteLine($"e.Node = {e.Node.DataItem.Title}|{e.Node.Class}");
    //    Console.WriteLine($"e.TargetNode ==> {e.TargetNode.DataItem.Title}|{e.TargetNode.Class}");

    //    if (e.TargetNode != null && e.TargetNode != e.Node)
    //    {
    //        e.Node.DataItem.ParentId = e.TargetNode.DataItem.Id;
    //    }

    //}

    void NodePropArrowClick(string nav)
    {
        var items = f.Model.MenuItems.ToList();

        if (_selMenu is null) throw new ArgumentNullException(nameof(_selMenu));

        if (nav == "left")
        {
            if (_selMenu.ParentId == Guid.Empty) return;
            Guid newParentId = Guid.Empty;
            var parentOfParent = Parent();
            if (parentOfParent is not null)
            {
                _selMenu.ParentId = parentOfParent.ParentId;
            }

        }
        else if (nav == "right")
        {
            var soseds = GetNegibors().ToList();
            var index = soseds.IndexOf(_selMenu);
            if (index > 0)
            {
                var prev = soseds[index - 1];
                _selMenu.ParentId = prev.Id;

            }
        }
        else if (nav == "up")
        {
            var soseds = GetNegibors().ToList();
            var index = soseds.IndexOf(_selMenu);
            if (index > 0)
            {
                var prev = soseds[index - 1];
                int globalIndex = items.IndexOf(prev);

                items.Remove(_selMenu);
                items.Insert(globalIndex, _selMenu);

            }
        }
        else if (nav == "down")
        {
            var soseds = GetNegibors().ToList();
            var index = soseds.IndexOf(_selMenu);
            var count = soseds.Count;
            if (index < count - 1)
            {
                var next = soseds[index + 1];
                int globalIndex = items.IndexOf(next);

                items.Remove(_selMenu);
                items.Insert(globalIndex, _selMenu);

            }
        }
        f.Model.MenuItems = items;
        //_selNode = f.Model.tre
    }

    NavMenuItem? Parent(Guid? id = null)
    {
        return _selMenu!.ParentId == Guid.Empty ? null : f.Model.MenuItems.FirstOrDefault(s => s.Id == (id ?? _selMenu.ParentId));
    }

    IEnumerable<NavMenuItem> GetNegibors()
    {
        var parent = Parent();
        if (parent is null)
        {
            return f.Model.MenuItems.Where(s => s.ParentId == Guid.Empty);
        }
        else
        {
            return parent.GetItems(f.Model);
        }
    }

    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(f.Model.Slug) || Guid.TryParse(f.Model.Slug, out Guid _))
        {
            f.Model.Slug = TextTool.TranslateToPostSlug(f.Model.Title);
        }
    }

    async void OnClickIconSelectMedia()
    {
        var file = await mediaService.OpenSelectMedia();

        if (file is not null && _selMenu is not null)
        {
            _selMenu!.Icon = file.UrlRelative;
            StateHasChanged();
        }
    }

    /////////////EXPORT IMPORT
    bool importButtonDisabled;
    string url = "";
    string import_json = "";
    bool visibleImportModal;

    void initExport()
    {
        url = Q.ServerUrlJoin($"/api/NavMenu/Export/{ID}");
    }

    void ShowImportModal()
    {
        visibleImportModal = true;
    }

    void ImportModalOnCancel()
    {
        visibleImportModal = false;
    }

    private async void LoadFiles(InputFileChangeEventArgs e)
    {
        importButtonDisabled = true;
        StateHasChanged();

        using MemoryStream ms = new MemoryStream();
        await e.File.OpenReadStream().CopyToAsync(ms);
        var bytes = ms.ToArray();
        string json = System.Text.Encoding.UTF8.GetString(bytes);
        import_json = json;
        //importVal = JsonConvert.DeserializeObject<SystemImportSettingsFile_v1>(import_json);

        importButtonDisabled = false;
        StateHasChanged();

    }

    async void OnImportClick()
    {
        //importVal = JsonConvert.DeserializeObject<SystemImportSettingsFile_v1>(import_json);
        //var result = await viewModelService.SystemImportSettings(importVal);
        var result = await client.NavMenu.Import(ID, import_json);

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
            _ = f.Load();
        }
        else
        {
            _ = messageService.Error(result.Message);
        }
    }
}
