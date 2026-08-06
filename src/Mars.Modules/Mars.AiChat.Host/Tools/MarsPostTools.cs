using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars.Core.Features;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: работа с постами через IPostService (без открытой страницы).
/// Экземпляр создаётся на каждый запуск агента с userId автора (владелец чата).
/// </summary>
public class MarsPostTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly IPostService _postService;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly Guid _userId;

    public MarsPostTools(IPostService postService, IHubContext<ChatHub> chatHub, Guid userId)
    {
        _postService = postService;
        _chatHub = chatHub;
        _userId = userId;
    }

    [Description("Создать новый пост БЕЗ открытия страницы. contentText — обычный текст поста: " +
                 "для блочного редактора он будет разбит на абзацы, для WYSIWYG обёрнут в <p>, иначе сохранён как есть. " +
                 "Slug генерируется из названия автоматически; пост создаётся в статусе по умолчанию (черновик).")]
    public async Task<string> CreatePost(
        [Description("Имя типа поста, например 'post' или 'page'")] string type,
        [Description("Название поста (title)")] string title,
        [Description("Текст поста (plain text)")] string contentText,
        [Description("Теги через запятую. Пустая строка — без тегов.")] string tagsCsv = "",
        [Description("Краткий анонс (excerpt). Пустая строка — без анонса.")] string excerpt = "")
    {
        try
        {
            var blank = await _postService.GetEditModelBlank(type, CancellationToken.None);
            var contentType = blank.PostType.PostContentSettings.PostContentType;

            var content = contentType switch
            {
                PostTypeConstants.DefaultPostContentTypes.BlockEditor => BuildBlockEditorJson(contentText),
                PostTypeConstants.DefaultPostContentTypes.WYSIWYG => BuildHtml(contentText),
                _ => contentText,
            };

            var query = new CreatePostQuery
            {
                Id = null,
                Title = title,
                Type = type,
                Slug = TextTool.TranslateToPostSlug(title),
                Tags = tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                UserId = _userId,
                Status = blank.Post?.Status, // статус по умолчанию для типа (draft); пустой, если фича Status выключена
                Content = content,
                Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt,
                LangCode = blank.Post?.LangCode ?? "",
                CategoryIds = [],
                MetaValues = [],
            };

            var created = await _postService.Create(query, CancellationToken.None);

            // Оповещаем админ-клиент, чтобы открытая страница списка постов обновила таблицу.
            await _chatHub.Clients.All.SendAsync(AdminHubEvents.PostListChanged, type, CancellationToken.None);

            return JsonSerializer.Serialize(new
            {
                id = created.Id,
                title = created.Title,
                slug = created.Slug,
                type = created.Type,
                status = created.Status,
                tags = created.Tags,
            }, SerializerOptions)
            + $" Пост создан (черновик). Страница редактирования в админке: /EditPost/{type}/{created.Id}";
        }
        catch (Exception ex)
        {
            return "Не удалось создать пост: " + ex.GetBaseException().Message;
        }
    }

    [Description("Прочитать пост по Guid: название, slug, тип, статус, теги и текст " +
                 "(content — как хранится, contentText — plain-text извлечение).")]
    public async Task<string> GetPost(
        [Description("Guid поста")] Guid postId)
    {
        try
        {
            var detail = await _postService.GetDetail(postId, renderContent: false, CancellationToken.None);
            if (detail is null)
                return $"Пост '{postId}' не найден.";

            return JsonSerializer.Serialize(new
            {
                id = detail.Id,
                title = detail.Title,
                slug = detail.Slug,
                type = detail.Type,
                status = detail.Status,
                tags = detail.Tags,
                content = detail.Content,
                contentText = ExtractPlainText(detail.Content),
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
