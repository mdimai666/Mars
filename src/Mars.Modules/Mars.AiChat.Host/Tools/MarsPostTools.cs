using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostJsons;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Hubs;
using Mars.Core.Features;
using Mars.Forms.Contracts;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Nodes.Abstractions.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: работа с постами через серверные сервисы CMS (без открытой страницы).
/// Запись идёт через JSON-путь (<see cref="IPostJsonService"/>) — он несёт метаполя целиком,
/// поэтому update безопасен для значений полей. Экземпляр создаётся на каждый запуск агента
/// с userId владельца чата — он становится автором новых постов.
/// </summary>
public class MarsPostTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly IPostService _postService;
    private readonly IPostJsonService _postJson;
    private readonly IMetaModelTypesLocator _typesLocator;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly Guid _userId;

    public MarsPostTools(IPostService postService, IPostJsonService postJson,
                         IMetaModelTypesLocator typesLocator, IHubContext<ChatHub> chatHub, Guid userId)
    {
        _postService = postService;
        _postJson = postJson;
        _typesLocator = typesLocator;
        _chatHub = chatHub;
        _userId = userId;
    }

    [Description("Описание типа поста: фичи, статусы (slug), редактор контента, поле картинки поста " +
                 "и все метаполя с типами, кратностью, обязательностью и вариантами Select. " +
                 "Вызывай перед созданием или обновлением поста с метаполями.")]
    public string DescribePostType(
        [Description("Имя типа поста, например 'post' или 'page'")] string type)
    {
        try
        {
            var postType = _typesLocator.GetPostTypeByName(type);
            if (postType is null) return NotFoundTypeMessage(type);

            var result = new
            {
                type = postType.TypeName,
                title = postType.Title,
                features = postType.EnabledFeatures,
                imageFieldKey = postType.ImageFieldKey,
                contentEditor = postType.ContentEditorKey(),
                statuses = postType.PostStatusList
                    .Where(_ => postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status))
                    .Select(s => new { slug = s.Slug, title = s.Title })
                    .ToArray(),
                metaFields = postType.MetaFields.Select(f => new
                {
                    key = f.Key,
                    title = f.Title,
                    type = f.Type.ToString(),
                    multiple = f.IsMultiple,
                    required = !f.IsNullable,
                    hidden = f.Hidden,
                    // вычислимые (Query) и выключенные поля значение не принимают
                    readOnly = f.Disabled || f.Type == MetaFieldType.Query,
                    modelName = f.ModelName,
                    variants = f.Variants?.Select(v => new { key = v.Key, title = v.Title }).ToArray(),
                }).ToArray(),
            };

            return JsonSerializer.Serialize(result, SerializerOptions);
        }
        catch (Exception ex)
        {
            return "Не удалось описать тип поста: " + ex.GetBaseException().Message;
        }
    }

    [Description("Создать новый пост БЕЗ открытия страницы. contentText — обычный текст поста: " +
                 "для блочного редактора он будет разбит на абзацы, для WYSIWYG обёрнут в <p>, иначе сохранён как есть. " +
                 "Slug генерируется из названия автоматически; пост создаётся в статусе по умолчанию (черновик). " +
                 "Метаполя — через metaJson; состав полей типа узнай через DescribePostType.")]
    public async Task<string> CreatePost(
        [Description("Имя типа поста, например 'post' или 'page'")] string type,
        [Description("Название поста (title)")] string title,
        [Description("Текст поста (plain text)")] string contentText,
        [Description("Теги через запятую. Пустая строка — без тегов.")] string tagsCsv = "",
        [Description("Краткий анонс (excerpt). Пустая строка — без анонса.")] string excerpt = "",
        [Description("Значения метаполей JSON-объектом «ключ поля: значение» (форматы — в DescribePostType). " +
                     "Пустая строка — без метаполей.")] string metaJson = "",
        [Description("Slug статуса (из DescribePostType). Пустая строка — статус по умолчанию.")] string status = "")
    {
        try
        {
            if (!TryNormalizeMeta(metaJson, type, out var meta, out var metaError)) return metaError!;

            var blank = await _postService.GetEditModelBlank(type, CancellationToken.None);
            var content = AdaptContent(contentText, blank.Form.Field(SystemFieldsCatalog.Content)?.Field?.Editor ?? "");

            var query = new CreatePostJsonQuery
            {
                Id = null,
                Title = title,
                Type = type,
                Slug = TextTool.TranslateToPostSlug(title),
                Tags = tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                UserId = _userId,
                // статус по умолчанию для типа (draft) резолвит JSON-сервис; пустой, если фича Status выключена
                Status = string.IsNullOrWhiteSpace(status) ? blank.Post?.Status : status,
                Content = content,
                Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt,
                LangCode = blank.Post?.LangCode ?? "",
                CategoryIds = [],
                Meta = meta,
            };

            var created = await _postJson.Create(query, CancellationToken.None);

            // Оповещаем админ-клиент, чтобы открытая страница списка постов обновила таблицу.
            await _chatHub.Clients.All.SendAsync(AdminHubEvents.PostListChanged, type, CancellationToken.None);

            return JsonSerializer.Serialize(new
            {
                id = created.Id,
                title = created.Title,
                slug = created.Slug,
                type = created.Type,
                status = created.Status?.Key,
                tags = created.Tags,
            }, SerializerOptions)
            + $" Пост создан. Страница редактирования в админке: /EditPost/{type}/{created.Id}";
        }
        catch (Exception ex)
        {
            return "Не удалось создать пост: " + ex.GetBaseException().Message;
        }
    }

    [Description("Обновить существующий пост БЕЗ открытия страницы: передай только меняемые поля, " +
                 "остальные сохранятся как есть. Перед правкой прочитай пост (GetPost). " +
                 "Внимание: запись заменяет данные целиком последними прочитанными значениями " +
                 "(last-write-wins) — не используй, пока пользователь правит пост в админке.")]
    public async Task<string> UpdatePost(
        [Description("Guid поста")] Guid postId,
        [Description("Новый заголовок. Пустая строка — не менять.")] string title = "",
        [Description("Новый текст поста (plain text) — адаптируется под редактор типа. Пустая строка — не менять.")] string contentText = "",
        [Description("Теги через запятую. Пустая строка — не менять; '-' — снять все теги.")] string tagsCsv = "",
        [Description("Краткий анонс. Пустая строка — не менять; '-' — убрать анонс.")] string excerpt = "",
        [Description("Slug статуса (из DescribePostType). Пустая строка — не менять.")] string status = "",
        [Description("Значения метаполей JSON-объектом — только изменяемые поля. Пустая строка — метаполя не трогать.")] string metaJson = "")
    {
        try
        {
            var dto = await _postJson.GetDetail(postId, renderContent: false, CancellationToken.None);
            if (dto is null) return $"Пост '{postId}' не найден.";

            if (!TryNormalizeMeta(metaJson, dto.Type, out var meta, out var metaError)) return metaError!;

            var postType = _typesLocator.GetPostTypeByName(dto.Type);
            if (postType is null) return NotFoundTypeMessage(dto.Type);

            var content = dto.Content;
            if (!string.IsNullOrEmpty(contentText))
                content = AdaptContent(contentText, postType.ContentEditorKey());

            IReadOnlyCollection<string> tags = tagsCsv switch
            {
                "" => dto.Tags,
                "-" => [],
                _ => tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            };

            var query = new UpdatePostJsonQuery
            {
                Id = dto.Id,
                Title = string.IsNullOrEmpty(title) ? dto.Title : title,
                Type = dto.Type,
                Slug = dto.Slug,
                Tags = tags,
                // автор не меняется: владелец чата редактирует, но пост остаётся за исходным автором
                UserId = dto.Author.Id,
                Content = content,
                Status = string.IsNullOrEmpty(status) ? dto.Status?.Key : status,
                Excerpt = excerpt switch
                {
                    "" => dto.Excerpt,
                    "-" => null,
                    _ => excerpt,
                },
                LangCode = dto.LangCode,
                CategoryIds = dto.Categories?.Select(c => c.Id).ToArray() ?? [],
                Meta = meta,
            };

            var updated = await _postJson.Update(query, CancellationToken.None);

            await _chatHub.Clients.All.SendAsync(AdminHubEvents.PostListChanged, dto.Type, CancellationToken.None);

            return JsonSerializer.Serialize(new
            {
                id = updated.Id,
                title = updated.Title,
                slug = updated.Slug,
                type = updated.Type,
                status = updated.Status?.Key,
                tags = updated.Tags,
                excerpt = updated.Excerpt,
            }, SerializerOptions)
            + $" Пост обновлён. Страница редактирования в админке: /EditPost/{dto.Type}/{dto.Id}";
        }
        catch (Exception ex)
        {
            return "Не удалось обновить пост: " + ex.GetBaseException().Message;
        }
    }

    [Description("Прочитать пост по Guid: название, slug, тип, статус, теги, анонс, язык, поле картинки типа, " +
                 "значения метаполей и текст (content — как хранится, contentText — plain-text извлечение).")]
    public async Task<string> GetPost(
        [Description("Guid поста")] Guid postId)
    {
        try
        {
            var dto = await _postJson.GetDetail(postId, renderContent: false, CancellationToken.None);
            if (dto is null) return $"Пост '{postId}' не найден.";

            var postType = _typesLocator.GetPostTypeByName(dto.Type);
            var meta = dto.Meta.ToDictionary(kv => kv.Key, kv => CompactMetaValue(kv.Value));

            return JsonSerializer.Serialize(new
            {
                id = dto.Id,
                title = dto.Title,
                slug = dto.Slug,
                type = dto.Type,
                status = dto.Status?.Key,
                tags = dto.Tags,
                excerpt = dto.Excerpt,
                langCode = dto.LangCode,
                imageFieldKey = postType?.ImageFieldKey,
                content = dto.Content,
                contentText = ExtractPlainText(dto.Content),
                meta,
            }, SerializerOptions);
        }
        catch (Exception ex)
        {
            return "Не удалось прочитать пост: " + ex.GetBaseException().Message;
        }
    }

    [Description("Список последних постов (id, название, slug, тип). Можно фильтровать по типу поста.")]
    public async Task<string> ListPosts(
        [Description("Имя типа поста, например 'post'. Пустая строка — все типы.")] string type = "",
        [Description("Максимум записей (1-50)")] int take = 10)
    {
        try
        {
            var query = new ListPostQuery
            {
                Type = string.IsNullOrWhiteSpace(type) ? null : type,
                Take = Math.Clamp(take, 1, 50),
            };

            var result = await _postService.List(query, CancellationToken.None);

            var items = result.Items.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                slug = p.Slug,
                type = p.Type,
            });

            return JsonSerializer.Serialize(items, SerializerOptions);
        }
        catch (Exception ex)
        {
            return "Не удалось получить список постов: " + ex.GetBaseException().Message;
        }
    }

    //=====================================

    string NotFoundTypeMessage(string type)
        => $"Тип поста '{type}' не найден. Доступные типы: {string.Join(", ", _typesLocator.PostTypesDict().Keys)}";

    /// <summary>Текст metaJson → словарь значений в строгой форме JSON-пути; пустой текст → null (не трогать метаполя)</summary>
    bool TryNormalizeMeta(string metaJson, string type, out Dictionary<string, JsonNode>? meta, out string? error)
    {
        meta = null;
        error = null;

        if (string.IsNullOrWhiteSpace(metaJson)) return true;

        var postType = _typesLocator.GetPostTypeByName(type);
        if (postType is null)
        {
            error = NotFoundTypeMessage(type);
            return false;
        }

        if (!MetaJsonNormalizer.TryParseObject(metaJson, out var raw, out error)) return false;
        if (!MetaJsonNormalizer.TryNormalize(raw, postType.MetaFields, out var normalized, out error)) return false;

        meta = normalized;
        return true;
    }

    /// <summary>Plain-текст под редактор контента типа: блочный — абзацами, WYSIWYG — html-абзацами</summary>
    static string AdaptContent(string contentText, string contentEditor) => contentEditor switch
    {
        FormEditorCatalog.BlockEditor => BuildBlockEditorJson(contentText),
        FormEditorCatalog.Wysiwyg => BuildHtml(contentText),
        _ => contentText,
    };

    /// <summary>
    /// Материализованные значения метаполей — компактно для модели: варианты — key/title,
    /// файлы — id/name/url, прочее (скаляры, dto связей) — как есть.
    /// </summary>
    static object? CompactMetaValue(object? value) => value switch
    {
        null => null,
        string text => text,
        MetaFieldVariantValueDto variant => new { key = variant.Key, title = variant.Title },
        FileDetail file => new { id = file.Id, name = file.Name, url = file.UrlRelative },
        System.Collections.IEnumerable sequence => sequence.Cast<object?>().Select(CompactMetaValue).ToArray(),
        _ => value,
    };

    /// <summary>
    /// Editor.js JSON из обычного текста: каждая непустая строка — абзац.
    /// </summary>
    static string BuildBlockEditorJson(string text)
    {
        var blocks = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => new { type = "paragraph", data = new { text = line } })
            .Cast<object>()
            .ToList();

        if (blocks.Count == 0)
            blocks.Add(new { type = "paragraph", data = new { text = "" } });

        return JsonSerializer.Serialize(new
        {
            time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            blocks,
            version = "2.31.0",
        }, SerializerOptions);
    }

    static string BuildHtml(string text)
    {
        var paragraphs = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => $"<p>{line}</p>");

        return string.Join("", paragraphs);
    }

    /// <summary>
    /// Plain-text извлечение: Editor.js-блоки или текст с удалением HTML-тегов.
    /// </summary>
    static string ExtractPlainText(string? content)
    {
        if (string.IsNullOrEmpty(content)) return "";

        try
        {
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("blocks", out var blocks))
            {
                var sb = new StringBuilder();

                foreach (var block in blocks.EnumerateArray())
                {
                    var type = block.TryGetProperty("type", out var t) ? t.GetString() : "";
                    var data = block.TryGetProperty("data", out var d) ? d : default;

                    switch (type)
                    {
                        case "paragraph":
                        case "header":
                            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("text", out var text))
                                sb.AppendLine(text.GetString());
                            break;
                        case "list":
                            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("items", out var items))
                            {
                                foreach (var item in items.EnumerateArray())
                                    sb.AppendLine(item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText());
                            }
                            break;
                        default:
                            sb.AppendLine($"[{type}]");
                            break;
                    }
                }

                return sb.ToString().Trim();
            }

            return content;
        }
        catch
        {
            // не JSON (HTML, шаблон, plain) — убираем теги, если есть
            return content.Contains('<') ? Regex.Replace(content, "<[^>]+>", " ").Trim() : content;
        }
    }
}
