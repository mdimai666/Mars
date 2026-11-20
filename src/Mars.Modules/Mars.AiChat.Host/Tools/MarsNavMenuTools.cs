using System.ComponentModel;
using System.Text.Json;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: меню навигации CMS (в т.ч. сайдбар админки — системное меню со slug "dev").
/// Работают через singleton <see cref="INavMenuService"/> (кеш и уведомление админки уже внутри):
/// правка = read-modify-write полного меню, как при сохранении из редактора меню.
/// </summary>
public class MarsNavMenuTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly INavMenuService _navMenuService;

    public MarsNavMenuTools(INavMenuService navMenuService)
    {
        _navMenuService = navMenuService;
    }

    [Description("Список меню навигации сайта: id, название, slug, теги. Сайдбар админки — системное меню со slug 'dev'.")]
    public async Task<string> ListNavMenus()
    {
        try
        {
            var result = await _navMenuService.ListForAdmin(new() { Take = 100 }, CancellationToken.None);

            return JsonSerializer.Serialize(result.Items.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                slug = s.Slug,
                disabled = s.Disabled,
                tags = s.Tags,
            }), SerializerOptions);
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Меню навигации по slug со всеми пунктами (плоский список: parentId связывает пункты в дерево, " +
                 "порядок в списке = порядок отображения). Вызывай перед правкой пунктов. " +
                 "isSystem=true — пункт дефолтного системного меню: его нельзя удалить, только отключить (disabled).")]
    public async Task<string> GetNavMenu(
        [Description("Slug меню, например 'dev' (сайдбар админки)")] string slug)
    {
        try
        {
            var detail = await ResolveMenu(slug);
            if (detail is null) return NotFoundMessage(slug);

            return JsonSerializer.Serialize(new
            {
                id = detail.Id,
                title = detail.Title,
                slug = detail.Slug,
                disabled = detail.Disabled,
                tags = detail.Tags,
                isPersisted = detail.IsPersisted,
                menuItems = detail.MenuItems.Select(ItemJson),
            }, SerializerOptions);
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Добавить пункт в меню навигации (в конец списка; для подпункта — parentId существующего пункта). " +
                 "После изменения меню вызови RefreshInitialSiteData, чтобы сайдбар админки обновился без перезагрузки. " +
                 "Для страницы админ-фронта url = '/dev/p/{url из @page}'.")]
    public async Task<string> AddMenuItem(
        [Description("Slug меню, например 'dev'")] string slug,
        [Description("Название пункта")] string title,
        [Description("Ссылка пункта, например '/dev/p/contacts' или '/dev/Media'")] string url,
        [Description("Id родительского пункта для подпункта. Пустая строка — корневой пункт.")] string parentId = "",
        [Description("Иконка: имя FluentUI-иконки или относительный URL картинки. Пустая строка — без иконки.")] string icon = "",
        [Description("Открывать в новой вкладке")] bool openInNewTab = false,
        [Description("Создать пункт отключённым (не показывается)")] bool disabled = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title)) return "Ошибка: название пункта (title) не может быть пустым.";

            var detail = await ResolveMenu(slug);
            if (detail is null) return NotFoundMessage(slug);

            if (!TryParseParent(parentId, detail, out var parentGuid, out var parentError)) return parentError!;

            var item = new NavMenuItemDto
            {
                Id = Guid.NewGuid(),
                ParentId = parentGuid,
                Title = title,
                Url = url,
                Icon = icon,
                Roles = [],
                RolesInverse = false,
                Class = "",
                Style = "",
                OpenInNewTab = openInNewTab,
                Disabled = disabled,
                IsHeader = false,
                IsDivider = false,
            };

            await _navMenuService.Update(ToUpdateQuery(detail, [.. detail.MenuItems, item]), CancellationToken.None);

            return JsonSerializer.Serialize(new { id = item.Id, title = item.Title, url = item.Url, parentId = item.ParentId }, SerializerOptions)
                   + " Пункт добавлен, меню сохранено. Вызови RefreshInitialSiteData, чтобы сайдбар админки обновился.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Изменить пункт меню. Пустая строка у параметра = не менять; '-' у icon/rolesCsv = очистить. " +
                 "parentId: id другого пункта, 'root' — сделать корневым. Системный пункт (isSystem) удалить нельзя, " +
                 "но можно отключить: disabled='true'.")]
    public async Task<string> UpdateMenuItem(
        [Description("Slug меню, например 'dev'")] string slug,
        [Description("Id пункта (из GetNavMenu)")] string itemId,
        [Description("Новое название. Пустая строка — не менять.")] string title = "",
        [Description("Новая ссылка. Пустая строка — не менять.")] string url = "",
        [Description("Новая иконка. Пустая строка — не менять, '-' — убрать.")] string icon = "",
        [Description("Новый родитель: id пункта или 'root'. Пустая строка — не менять.")] string parentId = "",
        [Description("'true'/'false' — показать/скрыть пункт. Пустая строка — не менять.")] string disabled = "",
        [Description("'true'/'false' — открывать в новой вкладке. Пустая строка — не менять.")] string openInNewTab = "",
        [Description("Роли через запятую (например 'Admin'). Пустая строка — не менять, '-' — убрать ограничение.")] string rolesCsv = "")
    {
        try
        {
            var detail = await ResolveMenu(slug);
            if (detail is null) return NotFoundMessage(slug);

            if (!Guid.TryParse(itemId, out var itemGuid)) return $"Ошибка: itemId '{itemId}' не является Guid.";

            var items = detail.MenuItems.ToList();
            var index = items.FindIndex(s => s.Id == itemGuid);
            if (index < 0) return $"Ошибка: пункт '{itemId}' не найден в меню '{slug}'.";

            var item = items[index];

            if (!string.IsNullOrWhiteSpace(title)) item = item with { Title = title };
            if (!string.IsNullOrWhiteSpace(url)) item = item with { Url = url };
            if (icon == "-") item = item with { Icon = "" };
            else if (!string.IsNullOrWhiteSpace(icon)) item = item with { Icon = icon };

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                if (!TryParseParent(parentId, detail, out var parentGuid, out var parentError)) return parentError!;
                if (parentGuid == itemGuid) return "Ошибка: пункт не может быть родителем самому себе.";
                if (parentGuid != Guid.Empty && IsDescendant(detail, parentGuid, itemGuid))
                    return "Ошибка: нельзя переместить пункт внутрь его же подпункта (цикл).";
                item = item with { ParentId = parentGuid };
            }

            if (!string.IsNullOrWhiteSpace(disabled))
            {
                if (!TryParseBool(disabled, out var disabledValue)) return $"Ошибка: disabled='{disabled}' — ожидается 'true' или 'false'.";
                item = item with { Disabled = disabledValue };
            }

            if (!string.IsNullOrWhiteSpace(openInNewTab))
            {
                if (!TryParseBool(openInNewTab, out var newTabValue)) return $"Ошибка: openInNewTab='{openInNewTab}' — ожидается 'true' или 'false'.";
                item = item with { OpenInNewTab = newTabValue };
            }

            if (rolesCsv == "-") item = item with { Roles = [] };
            else if (!string.IsNullOrWhiteSpace(rolesCsv))
                item = item with { Roles = rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };

            items[index] = item;
            await _navMenuService.Update(ToUpdateQuery(detail, items), CancellationToken.None);

            return $"Пункт '{item.Title}' обновлён, меню сохранено. Вызови RefreshInitialSiteData, чтобы сайдбар админки обновился.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    [Description("Удалить пункт меню вместе с его подпунктами. Опасное действие — сначала подтверди у пользователя через AskUser. " +
                 "Системные пункты (isSystem=true) не удаляются — отключи их через UpdateMenuItem(disabled='true').")]
    public async Task<string> RemoveMenuItem(
        [Description("Slug меню, например 'dev'")] string slug,
        [Description("Id пункта (из GetNavMenu)")] string itemId)
    {
        try
        {
            var detail = await ResolveMenu(slug);
            if (detail is null) return NotFoundMessage(slug);

            if (!Guid.TryParse(itemId, out var itemGuid)) return $"Ошибка: itemId '{itemId}' не является Guid.";

            var item = detail.MenuItems.FirstOrDefault(s => s.Id == itemGuid);
            if (item is null) return $"Ошибка: пункт '{itemId}' не найден в меню '{slug}'.";

            if (item.IsSystem)
                return "Системный пункт нельзя удалить — отключи его: UpdateMenuItem(disabled='true').";

            var toRemove = new HashSet<Guid> { itemGuid };
            CollectChildren(detail, itemGuid, toRemove);

            var items = detail.MenuItems.Where(s => !toRemove.Contains(s.Id)).ToList();
            await _navMenuService.Update(ToUpdateQuery(detail, items), CancellationToken.None);

            return $"Пункт '{item.Title}' удалён{(toRemove.Count > 1 ? $" вместе с подпунктами ({toRemove.Count - 1} шт.)" : "")}. Меню сохранено. " +
                   "Вызови RefreshInitialSiteData, чтобы сайдбар админки обновился.";
        }
        catch (Exception ex)
        {
            return Error(ex);
        }
    }

    static object ItemJson(NavMenuItemDto item) => new
    {
        id = item.Id,
        parentId = item.ParentId,
        title = item.Title,
        url = item.Url,
        icon = item.Icon,
        roles = item.Roles,
        rolesInverse = item.RolesInverse,
        openInNewTab = item.OpenInNewTab,
        disabled = item.Disabled,
        isHeader = item.IsHeader,
        isDivider = item.IsDivider,
        isSystem = item.IsSystem,
    };

    // Меню ищется по slug через ListForAdmin: он подмешивает несохранённое системное меню dev
    async Task<NavMenuDetail?> ResolveMenu(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        var list = await _navMenuService.ListForAdmin(new() { Search = slug, Take = 100 }, CancellationToken.None);
        var summary = list.Items.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (summary is null) return null;

        return await _navMenuService.GetDetail(summary.Id, CancellationToken.None);
    }

    static bool TryParseParent(string parentId, NavMenuDetail detail, out Guid parentGuid, out string? error)
    {
        parentGuid = Guid.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(parentId) || parentId.Equals("root", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Guid.TryParse(parentId, out var parsed))
        {
            error = $"Ошибка: parentId '{parentId}' не является Guid (пустая строка или 'root' — корневой пункт).";
            return false;
        }

        if (detail.MenuItems.All(s => s.Id != parsed))
        {
            error = $"Ошибка: родительский пункт '{parentId}' не найден в меню '{detail.Slug}'.";
            return false;
        }

        parentGuid = parsed;
        return true;
    }

    static bool TryParseBool(string value, out bool result)
    {
        if (bool.TryParse(value, out result)) return true;
        result = false;
        return false;
    }

    static bool IsDescendant(NavMenuDetail detail, Guid candidate, Guid ancestor)
    {
        var current = detail.MenuItems.FirstOrDefault(s => s.Id == candidate);
        while (current is not null && current.ParentId != Guid.Empty)
        {
            if (current.ParentId == ancestor) return true;
            current = detail.MenuItems.FirstOrDefault(s => s.Id == current.ParentId);
        }
        return false;
    }

    static void CollectChildren(NavMenuDetail detail, Guid parentId, HashSet<Guid> acc)
    {
        foreach (var child in detail.MenuItems.Where(s => s.ParentId == parentId))
        {
            if (acc.Add(child.Id)) CollectChildren(detail, child.Id, acc);
        }
    }

    static UpdateNavMenuQuery ToUpdateQuery(NavMenuDetail detail, IReadOnlyCollection<NavMenuItemDto> items)
        => new()
        {
            Id = detail.Id,
            Title = detail.Title,
            Slug = detail.Slug,
            Disabled = detail.Disabled,
            Tags = detail.Tags,
            MenuItems = items,
            Class = detail.Class,
            Style = detail.Style,
            Roles = detail.Roles,
            RolesInverse = detail.RolesInverse,
        };

    static string NotFoundMessage(string slug)
        => $"Ошибка: меню со slug '{slug}' не найдено. Список меню — ListNavMenus.";

    static string Error(Exception ex)
        => "Ошибка: " + ex.GetBaseException().Message;
}