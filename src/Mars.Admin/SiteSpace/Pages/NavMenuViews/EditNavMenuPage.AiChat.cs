using System.Text.Json;
using Mars.AiChat.Front.Services;

namespace Mars.Admin.SiteSpace.Pages.NavMenuViews;

/// <summary>
/// Мост ИИ-агента (IAiChatPageHandler): чтение и правка меню в открытой форме редактирования.
/// Правки попадают в форму БЕЗ сохранения — пользователь проверяет и жмёт «Сохранить»
/// (Save — только по явной просьбе). При открытой странице этот путь приоритетнее
/// серверных инструментов меню (скилл mars-admin-pages).
/// </summary>
public partial class EditNavMenuPage : IAiChatPageHandler, IDisposable
{
    private static readonly JsonSerializerOptions AiJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            AiChatPageHandlerHolder.Current = this;
        }
    }

    public void Dispose()
    {
        if (ReferenceEquals(AiChatPageHandlerHolder.Current, this))
            AiChatPageHandlerHolder.Current = null;
    }

    public string GetInfo()
    {
        var model = f?.Model;

        return JsonSerializer.Serialize(new
        {
            page = "EditNavMenu",
            menuId = ID,
            slug = model?.Slug,
            title = model?.Title,
            isSystem = model?.IsSystem ?? false,
            isPersisted = model?.IsPersisted ?? true,
            fields = new[] { "title", "slug", "class", "style", "disabled", "rolesInverse", "roles", "tags", "menuItems" },
            note = "Правки через SetOpenPageField попадают в форму без сохранения; menuItems — полная замена списка пунктов " +
                   "(JSON-массив, сначала прочитай GetOpenPageFields). Системные пункты (isSystem) не удаляй, только отключай. " +
                   "SaveOpenPage — только по явной просьбе пользователя.",
        }, AiJsonOptions);
    }

    public Task<string> GetFields()
    {
        var model = f?.Model;
        if (model is null || model.Id == Guid.Empty)
            return Task.FromResult("Ошибка: меню ещё не загружено.");

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            title = model.Title,
            slug = model.Slug,
            @class = model.Class,
            style = model.Style,
            disabled = model.Disabled,
            rolesInverse = model.RolesInverse,
            roles = model.Roles,
            tags = model.Tags,
            menuItems = model.MenuItems.Select(s => new
            {
                id = s.Id,
                parentId = s.ParentId,
                title = s.Title,
                url = s.Url,
                icon = s.Icon,
                roles = s.Roles,
                rolesInverse = s.RolesInverse,
                openInNewTab = s.OpenInNewTab,
                disabled = s.Disabled,
                isHeader = s.IsHeader,
                isDivider = s.IsDivider,
                isSystem = s.IsSystem,
            }),
        }, AiJsonOptions));
    }

    public async Task<string> SetField(string field, string value)
    {
        var model = f?.Model;
        if (model is null || model.Id == Guid.Empty)
            return "Ошибка: меню ещё не загружено.";

        switch (field)
        {
            case "title":
                if (string.IsNullOrWhiteSpace(value)) return "Ошибка: название меню не может быть пустым.";
                model.Title = value;
                break;
            case "slug":
                if (string.IsNullOrWhiteSpace(value)) return "Ошибка: slug не может быть пустым.";
                if (model.IsSystem) return "Системное меню нельзя увести со slug 'dev' — сервер вернёт его обратно.";
                model.Slug = value;
                break;
            case "class":
                model.Class = value;
                break;
            case "style":
                model.Style = value;
                break;
            case "disabled":
                if (!bool.TryParse(value, out var disabled)) return "Ошибка: disabled — ожидается 'true' или 'false'.";
                model.Disabled = disabled;
                break;
            case "rolesInverse":
                if (!bool.TryParse(value, out var rolesInverse)) return "Ошибка: rolesInverse — ожидается 'true' или 'false'.";
                model.RolesInverse = rolesInverse;
                break;
            case "roles":
                if (!TryParseList(value, out var roles)) return "Ошибка: roles — JSON-массив строк или CSV.";
                model.Roles = roles;
                break;
            case "tags":
                if (!TryParseList(value, out var tags)) return "Ошибка: tags — JSON-массив строк или CSV.";
                if (model.IsSystem && !tags.Contains("system")) return "У системного меню нельзя убрать тег 'system'.";
                model.Tags = tags.ToArray();
                break;
            case "menuItems":
                List<NavMenuItem>? items;
                try
                {
                    items = JsonSerializer.Deserialize<List<NavMenuItem>>(value, AiJsonOptions);
                }
                catch (JsonException ex)
                {
                    return "Ошибка: menuItems — JSON-массив пунктов: " + ex.Message;
                }
                if (items is null) return "Ошибка: menuItems — JSON-массив пунктов.";

                // isSystem вычисляется сервером и моделью не передаётся — сохраняем признак по id
                var systemIds = model.MenuItems.Where(s => s.IsSystem).Select(s => s.Id).ToHashSet();
                foreach (var item in items) item.IsSystem = systemIds.Contains(item.Id);

                model.MenuItems = items;
                if (_selMenu is not null && items.All(s => s.Id != _selMenu.Id)) _selMenu = null;
                break;
            default:
                return $"Ошибка: неизвестное поле '{field}'. Доступные: title, slug, class, style, disabled, rolesInverse, roles, tags, menuItems.";
        }

        await InvokeAsync(StateHasChanged);
        return $"Поле '{field}' изменено в форме (без сохранения). Пользователь проверит и сохранит сам.";
    }

    public async Task<string> Save()
    {
        if (f is null) return "Ошибка: форма ещё не инициализирована.";

        var saved = await f.Save();
        return saved
            ? "Меню сохранено. Сайдбар админки обновится автоматически."
            : "Не удалось сохранить меню: форма не прошла валидацию или сервер вернул ошибку (см. сообщение на странице).";
    }

    static bool TryParseList(string value, out List<string> result)
    {
        value = value.Trim();
        if (value.StartsWith('['))
        {
            try
            {
                result = JsonSerializer.Deserialize<List<string>>(value, AiJsonOptions) ?? [];
                return true;
            }
            catch (JsonException)
            {
                result = [];
                return false;
            }
        }

        result = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return true;
    }
}