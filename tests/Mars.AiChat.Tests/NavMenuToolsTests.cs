using System.Text.Json;
using FluentAssertions;
using Mars.AiChat.Host.Tools;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.Common;

namespace Mars.AiChat.Tests;

public class NavMenuToolsTests
{
    readonly FakeNavMenuService _service = new();
    readonly MarsNavMenuTools _tools;

    public NavMenuToolsTests()
    {
        _tools = new MarsNavMenuTools(_service);
    }

    [Fact]
    public async Task ListNavMenus_ReturnsMenus()
    {
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev", "system"));
        _service.Menus.Add(Menu(Guid.NewGuid(), "footer"));

        var json = await _tools.ListNavMenus();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(2);
        doc.RootElement[0].GetProperty("slug").GetString().Should().Be("dev");
    }

    [Fact]
    public async Task GetNavMenu_BySlug_ReturnsItemsWithSystemFlag()
    {
        var menu = Menu(Guid.NewGuid(), "dev", "system");
        var systemItem = Item("Главная", "/dev/", isSystem: true);
        _service.Menus.Add(menu with { MenuItems = [systemItem] });

        var json = await _tools.GetNavMenu("dev");

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("slug").GetString().Should().Be("dev");
        var items = doc.RootElement.GetProperty("menuItems");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("isSystem").GetBoolean().Should().BeTrue();
        items[0].GetProperty("id").GetGuid().Should().Be(systemItem.Id);
    }

    [Fact]
    public async Task GetNavMenu_UnknownSlug_ReturnsError()
    {
        var result = await _tools.GetNavMenu("nope");

        result.Should().Contain("не найдено");
        _service.Updates.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMenuItem_Root_AppendsItemAndPreservesMenuFields()
    {
        var menu = Menu(Guid.NewGuid(), "dev", "system");
        var existing = Item("Главная", "/dev/");
        _service.Menus.Add(menu with { MenuItems = [existing], Title = "Dev admin menu" });

        var result = await _tools.AddMenuItem("dev", "Контакты", "/dev/p/contacts");

        result.Should().Contain("Пункт добавлен");
        _service.Updates.Should().HaveCount(1);

        var update = _service.Updates[0];
        update.Id.Should().Be(menu.Id);
        update.Title.Should().Be("Dev admin menu");
        update.Tags.Should().Contain("system");
        update.MenuItems.Should().HaveCount(2);
        update.MenuItems.First().Id.Should().Be(existing.Id);

        var added = update.MenuItems.Last();
        added.Title.Should().Be("Контакты");
        added.Url.Should().Be("/dev/p/contacts");
        added.ParentId.Should().Be(Guid.Empty);
        added.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task AddMenuItem_WithParent_SetsParentId()
    {
        var parent = Item("Разделы", "#razdels");
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [parent] });

        await _tools.AddMenuItem("dev", "Письма", "/dev/FeedbackList", parentId: parent.Id.ToString());

        var added = _service.Updates[0].MenuItems.Last();
        added.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task AddMenuItem_UnknownParent_ReturnsErrorWithoutUpdate()
    {
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev"));

        var result = await _tools.AddMenuItem("dev", "Пункт", "/dev/x", parentId: Guid.NewGuid().ToString());

        result.Should().Contain("не найден");
        _service.Updates.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMenuItem_TitleOnly_OtherFieldsUntouched()
    {
        var item = Item("Старое", "/dev/old", icon: "Home", disabled: true);
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [item] });

        var result = await _tools.UpdateMenuItem("dev", item.Id.ToString(), title: "Новое");

        result.Should().Contain("обновлён");
        var updated = _service.Updates[0].MenuItems.Single();
        updated.Title.Should().Be("Новое");
        updated.Url.Should().Be("/dev/old");
        updated.Icon.Should().Be("Home");
        updated.Disabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMenuItem_DisabledAndIconClear_Parsed()
    {
        var item = Item("Пункт", "/dev/x", icon: "Home", isSystem: true);
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [item] });

        await _tools.UpdateMenuItem("dev", item.Id.ToString(), disabled: "true", icon: "-");

        var updated = _service.Updates[0].MenuItems.Single();
        updated.Disabled.Should().BeTrue();
        updated.Icon.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMenuItem_MoveIntoOwnChild_Rejected()
    {
        var parent = Item("Родитель", "#parent");
        var child = Item("Ребёнок", "#child") with { ParentId = parent.Id };
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [parent, child] });

        var result = await _tools.UpdateMenuItem("dev", parent.Id.ToString(), parentId: child.Id.ToString());

        result.Should().Contain("цикл");
        _service.Updates.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMenuItem_InvalidBool_ReturnsError()
    {
        var item = Item("Пункт", "/dev/x");
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [item] });

        var result = await _tools.UpdateMenuItem("dev", item.Id.ToString(), disabled: "yes");

        result.Should().Contain("true");
        _service.Updates.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveMenuItem_RemovesItemWithChildren()
    {
        var parent = Item("Родитель", "#parent");
        var child = Item("Ребёнок", "#child") with { ParentId = parent.Id };
        var other = Item("Другой", "/dev/other");
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev") with { MenuItems = [parent, child, other] });

        var result = await _tools.RemoveMenuItem("dev", parent.Id.ToString());

        result.Should().Contain("удалён");
        _service.Updates[0].MenuItems.Should().ContainSingle().Which.Id.Should().Be(other.Id);
    }

    [Fact]
    public async Task RemoveMenuItem_SystemItem_Refused()
    {
        var item = Item("Главная", "/dev/", isSystem: true);
        _service.Menus.Add(Menu(Guid.NewGuid(), "dev", "system") with { MenuItems = [item] });

        var result = await _tools.RemoveMenuItem("dev", item.Id.ToString());

        result.Should().Contain("Системный пункт нельзя удалить");
        _service.Updates.Should().BeEmpty();
    }

    static NavMenuDetail Menu(Guid id, string slug, params string[] tags)
        => new()
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Title = slug,
            Slug = slug,
            Disabled = false,
            Tags = tags,
            ModifiedAt = null,
            MenuItems = [],
            Class = "",
            Style = "",
            Roles = [],
            RolesInverse = false,
        };

    static NavMenuItemDto Item(string title, string url, string? icon = "", bool disabled = false, bool isSystem = false)
        => new()
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.Empty,
            Title = title,
            Url = url,
            Icon = icon,
            Roles = [],
            RolesInverse = false,
            Class = "",
            Style = "",
            OpenInNewTab = false,
            Disabled = disabled,
            IsHeader = false,
            IsDivider = false,
            IsSystem = isSystem,
        };

    sealed class FakeNavMenuService : INavMenuService
    {
        public List<NavMenuDetail> Menus { get; } = [];
        public List<UpdateNavMenuQuery> Updates { get; } = [];

        public Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<NavMenuSummary?>(Menus.FirstOrDefault(m => m.Id == id));

        public Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Menus.FirstOrDefault(m => m.Id == id));

        public Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ListDataResult<NavMenuSummary>> ListForAdmin(ListNavMenuQuery query, CancellationToken cancellationToken)
        {
            var items = Menus
                .Where(m => query.Search is null
                            || m.Slug.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                            || m.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(new ListDataResult<NavMenuSummary>(items, false, items.Count));
        }

        public Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken)
        {
            Updates.Add(query);
            var index = Menus.FindIndex(m => m.Id == query.Id);
            if (index >= 0)
            {
                Menus[index] = Menus[index] with
                {
                    Title = query.Title,
                    Slug = query.Slug,
                    Disabled = query.Disabled,
                    Tags = query.Tags,
                    MenuItems = query.MenuItems,
                    Class = query.Class,
                    Style = query.Style,
                    Roles = query.Roles,
                    RolesInverse = query.RolesInverse,
                    IsPersisted = true,
                };
            }
            return Task.CompletedTask;
        }

        public Task<NavMenuSummary> Delete(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<NavMenuSummary>> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task Reset(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public NavMenuDetail DevMenu() => throw new NotSupportedException();

        public IReadOnlyCollection<NavMenuDetail> GetAppInitialDataMenus(bool includeDevMenu = false)
            => throw new NotSupportedException();

        public Task<NavMenuExport> Export(Guid id) => throw new NotSupportedException();

        public Task<UserActionResult> Import(Guid id, NavMenuImport navMenu) => throw new NotSupportedException();
    }
}