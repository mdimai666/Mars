using AppFront.Shared.Interfaces;
using AppFront.Shared.Models;
using AppFront.Shared.Services;
using Blazored.LocalStorage;
using Mars.AiChat.Front.Services;
using Mars.Shared.Contracts.Search;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Interfaces;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AppAdmin.Shared.ActionCenter;

public partial class ActionCenter : IDisposable
{
    const string MruStorageKey = "action-center.mru-commands";
    const int MruMax = 8;
    const int SearchDebounceMs = 150;

    [Inject] ActionCenterService Service { get; set; } = default!;
    [Inject] IMarsWebApiClient Client { get; set; } = default!;
    [Inject] IActAppService ActAppService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] ILocalStorageService LocalStorage { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] RecentPagesService RecentPages { get; set; } = default!;
    [Inject] IMessageService MessageService { get; set; } = default!;
    [Inject] IBlazorPagesService BlazorPages { get; set; } = default!;
    [Inject] IAiChatAppService AiChatService { get; set; } = default!;

    enum Mode { Default, Commands, Search }
    enum PaletteView { Main, Pages }

    string _search = "";
    bool _loading;
    bool _searching;
    bool _focusInput;
    PaletteView _view = PaletteView.Main;

    IReadOnlyCollection<XActionCommand> _commands = [];
    List<string> _mruIds = [];
    IReadOnlyCollection<SearchFoundElementResponse> _searchResults = [];
    List<BlazorPageInfo>? _pages;

    List<PaletteSection> _sections = [];
    List<PaletteItem> _flat = [];
    int _selected;

    CancellationTokenSource? _searchCts;
    int _searchSeq;

    ElementReference _inputRef;

    protected override void OnInitialized()
    {
        Service.StateChanged += OnServiceStateChanged;
    }

    public void Dispose()
    {
        Service.StateChanged -= OnServiceStateChanged;
        _searchCts?.Cancel();
    }

    void OnServiceStateChanged()
    {
        _ = InvokeAsync(async () =>
        {
            if (Service.IsOpen)
                await OpenAsync();
            else
            {
                _search = "";
                _view = PaletteView.Main;
                _sections = [];
                _flat = [];
                StateHasChanged();
            }
        });
    }

    async Task OpenAsync()
    {
        _search = "";
        _view = PaletteView.Main;
        _searchResults = [];
        _sections = [];
        _flat = [];
        _loading = true;
        StateHasChanged();

        try
        {
            await Task.WhenAll(
                LoadCommandsAsync(),
                LoadMruAsync(),
                RecentPages.EnsureInitializedAsync());

            RebuildList();
        }
        catch (Exception ex)
        {
            _ = MessageService.Error(ex.Message);
        }
        finally
        {
            _loading = false;
            _focusInput = true;
            StateHasChanged();
        }
    }

    async Task LoadCommandsAsync()
    {
        var dict = await Client.Act.List();
        _commands = dict.Values.ToList();
    }

    async Task LoadMruAsync()
    {
        try
        {
            _mruIds = await LocalStorage.GetItemAsync<List<string>>(MruStorageKey) ?? [];
        }
        catch
        {
            _mruIds = [];
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusInput)
        {
            _focusInput = false;
            // без этого ArrowUp/ArrowDown двигают каретку в инпуте при навигации по списку
            await JS.InvokeVoidAsync("d_actionCenter_preventArrowCaret", _inputRef);
            await _inputRef.FocusAsync();
        }
    }

    // ---------- input ----------

    void OnInput(ChangeEventArgs e)
    {
        _search = e.Value?.ToString() ?? "";
        OnSearchChanged();
    }

    void OnSearchChanged()
    {
        if (_view == PaletteView.Pages)
        {
            RebuildList();
            return;
        }

        var mode = DetectMode(_search);
        var query = StripPrefix(_search);

        if (mode == Mode.Search || (mode == Mode.Default && !string.IsNullOrWhiteSpace(query)))
        {
            _searching = true;
            _ = ScheduleSearchAsync(query);
        }
        else
        {
            // запрос очищен — гасим зависший поиск, чтобы он не перезаписал список позже
            _searchCts?.Cancel();
            _searchResults = [];
            _searching = false;
        }

        RebuildList();
    }

    async Task ScheduleSearchAsync(string query)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        var seq = ++_searchSeq;

        try
        {
            await Task.Delay(SearchDebounceMs, cts.Token);
            if (seq != _searchSeq) return;

            // токен передаётся в запрос: устаревший (вытесненный) запрос реально отменяется
            var results = await Client.Search.Query(query, cancellationToken: cts.Token);
            if (seq != _searchSeq) return;

            _searchResults = results;
            _searching = false;
            RebuildList();
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // запрос вытеснен более новым — его результат не нужен
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ActionCenter search error: {ex.Message}");
            if (seq == _searchSeq)
            {
                _searching = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // ---------- keyboard ----------

    void OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (_flat.Count > 0) _selected = Math.Min(_selected + 1, _flat.Count - 1);
                ScrollSelectedIntoView();
                break;
            case "ArrowUp":
                if (_flat.Count > 0) _selected = Math.Max(_selected - 1, 0);
                ScrollSelectedIntoView();
                break;
            case "Enter":
                if (_selected >= 0 && _selected < _flat.Count)
                    _ = ExecuteItemAsync(_flat[_selected]);
                break;
            case "Escape":
                if (_view == PaletteView.Pages)
                {
                    _view = PaletteView.Main;
                    _search = "";
                    RebuildList();
                }
                else
                {
                    Service.Close();
                }
                break;
        }
    }

    void ScrollSelectedIntoView()
    {
        _ = JS.InvokeVoidAsync("d_actionCenter_scrollIntoView", $"ac-item-{_selected}");
    }

    void OnOverlayClick()
    {
        Service.Close();
    }

    // ---------- building the list ----------

    static Mode DetectMode(string s)
        => s.StartsWith(">") ? Mode.Commands
         : s.StartsWith("#") ? Mode.Search
         : Mode.Default;

    static string StripPrefix(string s)
        => (s.StartsWith(">") || s.StartsWith("#")) ? s[1..].TrimStart() : s;

    void RebuildList()
    {
        if (_view == PaletteView.Pages)
        {
            RebuildPagesList();
            return;
        }

        var mode = DetectMode(_search);
        var query = StripPrefix(_search);

        _sections = mode switch
        {
            Mode.Commands => string.IsNullOrWhiteSpace(query)
                ? BuildAllCommandsSections()
                : BuildCommandSections(query),
            Mode.Search => BuildSearchSections(),
            _ => string.IsNullOrWhiteSpace(query)
                ? BuildEmptyQuerySections()
                : BuildTypingSections(query),
        };

        FlattenAndSelect();
    }

    void RebuildPagesList()
    {
        var query = _search.Trim();
        var pages = _pages ?? [];

        var filtered = string.IsNullOrWhiteSpace(query)
            ? pages
            : pages
                .Select(p => (Page: p, Score: MatchScore(PageSearchText(p), query)))
                .Where(x => x.Score is not null)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Page)
                .ToList();

        _sections =
        [
            new PaletteSection { Items = filtered.Select(PageItem).ToList() },
        ];

        FlattenAndSelect();
    }

    void FlattenAndSelect()
    {
        _flat = [];
        foreach (var section in _sections)
            foreach (var item in section.Items)
            {
                item.FlatIndex = _flat.Count;
                _flat.Add(item);
            }

        _selected = _flat.Count > 0 ? 0 : -1;
    }

    List<PaletteSection> BuildEmptyQuerySections()
    {
        var sections = new List<PaletteSection>
        {
            new() { Items = PinnedItems() },
        };

        var recentCommands = _mruIds
            .Select(id => _commands.FirstOrDefault(c => c.Id == id))
            .Where(c => c is not null)
            .Select(c => CommandItem(c!))
            .ToList();

        var recentPages = RecentPages.Pages
            .Select(RecentItem)
            .ToList();

        var recent = new List<PaletteItem>(recentCommands.Count + recentPages.Count);
        recent.AddRange(recentCommands);
        recent.AddRange(recentPages);

        if (recent.Count > 0)
            sections.Add(new PaletteSection { DividerBefore = true, Items = recent });

        return sections;
    }

    /// <summary>
    /// Режим «&gt;» без ввода: все команды (со скроллом), рекомендуемые — сверху.
    /// </summary>
    List<PaletteSection> BuildAllCommandsSections()
    {
        var recommended = _commands
            .Where(c => c.Recommended is > 0)
            .OrderByDescending(c => c.Recommended)
            .ToList();

        var recommendedIds = recommended.Select(c => c.Id).ToHashSet();

        var rest = _commands
            .Where(c => !recommendedIds.Contains(c.Id))
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<PaletteItem>(recommended.Count + rest.Count);
        items.AddRange(recommended.Select(CommandItem));
        items.AddRange(rest.Select(CommandItem));

        return [new PaletteSection { Items = items }];
    }

    List<PaletteSection> BuildTypingSections(string query)
    {
        var sections = new List<PaletteSection>();

        var commands = FilterCommands(query).Select(CommandItem).ToList();
        if (commands.Count > 0)
            sections.Add(new PaletteSection { Items = commands });

        var search = _searchResults.Select(SearchItem).ToList();
        if (search.Count > 0)
            sections.Add(new PaletteSection { DividerBefore = commands.Count > 0, Items = search });

        return sections;
    }

    List<PaletteSection> BuildCommandSections(string query)
    {
        var commands = FilterCommands(query).Select(CommandItem).ToList();
        return [new PaletteSection { Items = commands }];
    }

    List<PaletteSection> BuildSearchSections()
    {
        var search = _searchResults.Select(SearchItem).ToList();
        return [new PaletteSection { Items = search }];
    }

    IEnumerable<XActionCommand> FilterCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return _commands;

        return _commands
            .Select(c => (Command: c, Score: MatchScore(CommandSearchText(c), query)))
            .Where(x => x.Score is not null)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Command);
    }

    static string CommandSearchText(XActionCommand c)
        => $"{c.Label} {c.Category} {c.Id}";

    static string PageSearchText(BlazorPageInfo p)
        => $"{p.DisplayName} {string.Join(" ", p.Routes)}";

    /// <summary>
    /// Нечёткое совпадение: префикс &gt; подстрока &gt; подпоследовательность; null — нет совпадения.
    /// </summary>
    static int? MatchScore(string text, string query)
    {
        if (string.IsNullOrEmpty(query)) return 0;

        var t = text.ToLowerInvariant();
        var q = query.ToLowerInvariant();

        var idx = t.IndexOf(q, StringComparison.Ordinal);
        if (idx == 0) return 100;
        if (idx > 0) return 80;

        var position = 0;
        foreach (var ch in q)
        {
            var found = t.IndexOf(ch, position);
            if (found < 0) return null;
            position = found + 1;
        }
        return 40;
    }

    // ---------- items ----------

    static List<PaletteItem> PinnedItems() =>
    [
        new PaletteItem { Id = "pin:goto", Title = "Перейти на страницу", Kind = PaletteItemKind.Pinned, IconClass = "bi bi-window-stack", Pinned = PinnedCommand.GoToPage },
        new PaletteItem { Id = "pin:run", Title = "Выполнить команду", Description = ">", Kind = PaletteItemKind.Pinned, IconClass = "bi bi-terminal", Pinned = PinnedCommand.RunCommand },
        new PaletteItem { Id = "pin:search", Title = "Поиск", Description = "#", Kind = PaletteItemKind.Pinned, IconClass = "bi bi-search", Pinned = PinnedCommand.Search },
        new PaletteItem { Id = "pin:ai", Title = "Открыть чат с ИИ", Kind = PaletteItemKind.Pinned, IconClass = "bi bi-robot", Pinned = PinnedCommand.OpenAiChat },
        new PaletteItem { Id = "pin:nodes", Title = "Редактор нод", Kind = PaletteItemKind.Pinned, IconClass = "bi bi-diagram-3", Pinned = PinnedCommand.OpenNodesEditor },
    ];

    static PaletteItem CommandItem(XActionCommand command) => new()
    {
        Id = command.Id,
        Title = command.Label,
        Description = command.Description ?? command.Category,
        Kind = PaletteItemKind.Command,
        IconClass = "bi bi-terminal",
        Command = command,
    };

    static PaletteItem SearchItem(SearchFoundElementResponse result) => new()
    {
        Id = "search:" + result.Key,
        Title = result.Title,
        Description = result.Description ?? result.Url,
        Kind = PaletteItemKind.SearchResult,
        Url = result.Url,
        IconClass = "bi bi-file-earmark-text",
        SearchResult = result,
    };

    static PaletteItem RecentItem(RecentPage page) => new()
    {
        Id = "recent:" + page.Url,
        Title = page.Title ?? page.Url,
        Description = page.Title is null ? null : page.Url,
        Kind = PaletteItemKind.RecentPage,
        Url = page.Url,
        IconClass = "bi bi-clock-history",
    };

    static PaletteItem PageItem(BlazorPageInfo page)
    {
        var route = page.Routes.FirstOrDefault() ?? "";
        var url = route.StartsWith('/') ? "/dev" + route : "/dev/" + route;
        return new PaletteItem
        {
            Id = "page:" + route,
            Title = page.DisplayName,
            Description = route,
            Kind = PaletteItemKind.Page,
            Url = url,
            IconClass = "bi bi-window-stack",
        };
    }

    // ---------- pages sub-view ----------

    Task LoadPagesAsync()
    {
        if (_pages is not null) return Task.CompletedTask;

        var pages = BlazorPages.GetStaticRoutedPages([typeof(App).Assembly]);
        _pages = pages.Where(RoleAllowed).ToList();
        return Task.CompletedTask;
    }

    static bool RoleAllowed(BlazorPageInfo page)
    {
        if (page.Roles is null || page.Roles.Count == 0) return true;

        var userRoles = Q.User.Roles.Append("Viewer").ToHashSet(StringComparer.OrdinalIgnoreCase);
        return page.Roles.Intersect(userRoles, StringComparer.OrdinalIgnoreCase).Any();
    }

    // ---------- execution ----------

    async Task ExecuteItemAsync(PaletteItem item)
    {
        switch (item.Kind)
        {
            case PaletteItemKind.Pinned:
                await ExecutePinnedAsync(item.Pinned!.Value);
                break;

            case PaletteItemKind.Command:
                Service.Close();
                AddMru(item.Id);
                await ActAppService.Inject(item.Id);
                break;

            case PaletteItemKind.Page:
            case PaletteItemKind.SearchResult:
            case PaletteItemKind.RecentPage:
                Service.Close();
                if (!string.IsNullOrEmpty(item.Url))
                    NavigationManager.NavigateTo(item.Url);
                break;
        }
    }

    async Task ExecutePinnedAsync(PinnedCommand pinned)
    {
        switch (pinned)
        {
            case PinnedCommand.GoToPage:
                _view = PaletteView.Pages;
                _search = "";
                await LoadPagesAsync();
                RebuildList();
                _focusInput = true;
                StateHasChanged();
                break;

            case PinnedCommand.RunCommand:
                _search = ">";
                OnSearchChanged();
                _focusInput = true;
                StateHasChanged();
                break;

            case PinnedCommand.Search:
                _search = "#";
                OnSearchChanged();
                _focusInput = true;
                StateHasChanged();
                break;

            case PinnedCommand.OpenAiChat:
                Service.Close();
                AiChatService.Open();
                break;

            case PinnedCommand.OpenNodesEditor:
                Service.Close();
                NavigationManager.NavigateTo("/dev/nodered");
                break;
        }
    }

    async void AddMru(string id)
    {
        _mruIds.Remove(id);
        _mruIds.Insert(0, id);
        if (_mruIds.Count > MruMax)
            _mruIds = _mruIds.Take(MruMax).ToList();

        try
        {
            await LocalStorage.SetItemAsync(MruStorageKey, _mruIds);
        }
        catch
        {
            // некритично
        }
    }
}
