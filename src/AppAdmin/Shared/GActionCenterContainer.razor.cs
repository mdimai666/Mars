using AppFront.Shared.Interfaces;
using Mars.Shared.Contracts.Search;
using Mars.Shared.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Shared;

public partial class GActionCenterContainer
{
    [Inject] IMessageService _messageService { get; set; } = default!;
    [Inject] IActAppService _actAppService { get; set; } = default!;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] ViewModelService vm { get; set; } = default!;
    [Inject] DeveloperControlService _devControlService { get; set; } = default!;

    //List<LSearchFoundElement> locals { get; set; } = new();
    //List<LSearchFoundElement> remotes { get; set; } = new();
    //bool IsBusy = false;

    // string _search = default!;
    // [Parameter] public string Search { get => _search ?? ""; set { _search = value; Load(); } }

    string? _prevSearch;
    [Parameter] public string Search { get; set; } = default!;
    [Parameter] public bool Visible { get; set; }

    IEnumerable<RSearchFoundElement> Items = [];

    IReadOnlyCollection<RSearchFoundElement> _remoteItems = [];

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Search))
        {
            if (_prevSearch != Search)
            {
                _prevSearch = Search;
                Load();
            }

        }
    }

    async void Load()
    {
        if (!Visible) return;

        Items = LocalItems();

        //IsBusy = true;
        StateHasChanged();

        if (Search.Length > 1)
        {
            try
            {
                _remoteItems = [];
                var result = await vm.GlobalSearch(Search);
                _remoteItems = result.Select(RSearchFoundElement.FromResponse).ToList();
                Items = [.. Items, .. _remoteItems];
            }
            catch (Exception ex)
            {
                _ = _messageService.Error(ex.Message);
            }
        }

        //IsBusy = false;
        StateHasChanged();
    }

    //void OnResultItemClick(object item, MouseEventArgs e)
    //{
    //    Visible = false;
    //}

    void OnResultItemClick(RSearchFoundElement item)
    {
        if (item is LSearchFoundElement local)
        {

        }
        else
        {
        }

        if(item.Key == "source-code")
        {
            _devControlService.OpenPageSource(App.PageType);
        }
        else if (item.Type == FoundElementType.XAction)
        {
            _actAppService.Inject(item.Key, []);
        }
        else
        {
            if (string.IsNullOrEmpty(item.Url)) _ = _messageService.Warning("url is empty");
            else _navigationManager.NavigateTo(item.Url);
        }

        //_ = _messageService.Info(item.Title);
        Visible = false;
    }

    static readonly List<LSearchFoundElement> definedLocals = new()
    {
        LSearchFoundElement.Create("Настройки","settings", "Настройки сайта", "/dev/Settings", "Admin"),
        LSearchFoundElement.Create("Записи","posts", "список записей", "/Post/post", ""),
        LSearchFoundElement.Create("Управление пользователями","users", "список пользователей", "/Users", "Admin"),
        LSearchFoundElement.Create("Выйти","logout", "выход из авторизованного аккаунта", "/Logout", ""),
        LSearchFoundElement.Create("Node-Red","nodered", "конструктор визуального программирования", "/dev/nodered", ""),
        LSearchFoundElement.Create("Source code","source-code", "Open page source code", "#"),
    };

    List<LSearchFoundElement> LocalItems()
    {
        return definedLocals.Where(s => s.Contain(Search)).Take(4).Select(s => s.AsSearchMakedText(Search)).ToList();
    }

    //List<LSearchFoundElement> RemoteItems(List<RSearchFoundElement> ss)
    //{
    //    return ss.Select(s => new LSearchFoundElement(s.Title, s.Description, s.Url)
    //    {
    //        Id = s.Id,
    //        Title = s.Title,
    //        Description = s.Description,
    //        ModelName = s.ModelName,
    //        Url = UrlByModel(s)
    //    })
    //        .Select(s => s.AsSearchMakedText(Search))
    //        .ToList();
    //}

    //string UrlByModel(RSearchFoundElement s)
    //{
    //    return s.ModelName switch
    //    {
    //        nameof(PostSummaryResponse) => $"/news/{s.Id}",
    //        //nameof(FaqPost) => $"/news/{s.Id}",
    //        _ => ""
    //    };
    //}

    private class RSearchFoundElement
    {
        public required string Title { get; init; }
        public required string Key { get; init; }
        public required string? Description { get; init; }
        public required string? Url { get; init; }
        public required float Relevant { get; init; }

        public required FoundElementType Type { get; init; }
        public required IReadOnlyDictionary<string, string> Data { get; init; }

        public static RSearchFoundElement FromResponse(SearchFoundElementResponse response)
            => new()
            {
                Title = response.Title,
                Description = response.Description,
                Url = response.Url,
                Key = response.Key,
                Relevant = response.Relevant,
                Data = response.Data,
                Type = response.Type,
            };
    }

    private class LSearchFoundElement : RSearchFoundElement
    {
        public string[]? Roles = null;

        public static LSearchFoundElement Create(string title, string key, string desc, string url, string? roles = null)
        {
            return new()
            {
                Title = title,
                Description = desc,
                Url = url,
                Key = key,
                Relevant = 100,
                Data = new Dictionary<string, string>(),
                Type = FoundElementType.Page,
                Roles = string.IsNullOrEmpty(roles) ? null : roles?.Split(',')
            };
        }

        public bool Contain(string text)
        {
            if (string.IsNullOrEmpty(text) && Roles == null) return true;
            if (string.IsNullOrEmpty(text)) return inRole();

            return (Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || Description.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || Key.Contains(text, StringComparison.OrdinalIgnoreCase)
                    );
        }

        public LSearchFoundElement AsSearchMakedText(string text) => this;

        // public LSearchFoundElement AsSearchMakedText(string text)
        // {
        //     if (string.IsNullOrEmpty(text)) return this;

        //     return new LSearchFoundElement(
        //             title: Title.Replace(text, $"<mark>{text.ToLower()}</mark>", StringComparison.OrdinalIgnoreCase),
        //             desc: Description.Replace(text, $"<mark>{text.ToLower()}</mark>", StringComparison.OrdinalIgnoreCase),
        //             url: Url
        //         );
        // }

        bool inRole()
        {
            return Roles?.Any(s => !string.IsNullOrEmpty(s) && Q.User.Roles.Contains(s)) ?? true;
        }
    }
}
