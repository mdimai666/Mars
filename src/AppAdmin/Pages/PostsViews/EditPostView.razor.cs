using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EditorJsBlazored;
using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;
using Mars.AiChat.Front.Services;
using Mars.Core.Features;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Interfaces;
using Mars.WebApiClient.Interfaces;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using static Mars.Shared.Contracts.PostTypes.PostTypeConstants;

namespace AppAdmin.Pages.PostsViews;

public partial class EditPostView : IAiChatPageHandler
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;
    [Inject] IAIToolAppService aiTool { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ID { get; set; }
    [Parameter, EditorRequired] public string PostTypeName { get; set; } = default!;

    StandartEditContainer<PostEditModel> f = default!;

    //OLD
    WysiwygEditor? editor1;
    CodeEditor2? codeEditor1;
    BlockEditor1? blockEditor1;

    string lang1 = CodeEditor2.Language.handlebars;

    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(f.Model.Slug) || Guid.TryParse(f.Model.Slug, out Guid _))
        {
            f.Model.Slug = TextTool.TranslateToPostSlug(f.Model.Title);
        }
    }

    async Task BeforeSave(PostEditModel post)
    {
        if (post.FeatureActivated(PostTypeConstants.Features.Content))
        {
            var contentType = post.PostType.PostContentSettings.PostContentType;

            if (contentType == PostTypeConstants.DefaultPostContentTypes.WYSIWYG)
            {

                if (editor1 is not null)
                {
                    post.Content = await editor1!.GetHTML();
                }
            }
            else if (contentType == PostTypeConstants.DefaultPostContentTypes.Code)
            {
                post.Content = await codeEditor1!.GetValue();

            }
        }
        //f.Model.Type = post.PostType.TypeName;

    }

    void OnSaveFromCodeEditor(string value)
    {
        f.Model.Content = value;
        _ = f.OnSubmit();
    }

    public void Dispose()
    {
        if (ReferenceEquals(AiChatPageHandlerHolder.Current, this))
            AiChatPageHandlerHolder.Current = null;

        codeEditor1?.Dispose();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            AiChatPageHandlerHolder.Current = this;
        }
    }

    // ---------- IAiChatPageHandler: инструменты ИИ-агента на открытой странице ----------

    private static readonly JsonSerializerOptions AiJsonOptions = new() { WriteIndented = false };

    public string GetInfo()
    {
        return JsonSerializer.Serialize(new
        {
            page = "EditPost",
            postType = PostTypeName,
            postId = f?.Model?.Id ?? ID,
            contentType = PostContentType,
            fields = new[] { "title", "slug", "excerpt", "tags", "categories", "content" },
            contentAiEditable = PostContentType != DefaultPostContentTypes.WYSIWYG,
        }, AiJsonOptions);
    }

    public async Task<string> GetFields()
    {
        var model = f?.Model ?? throw new InvalidOperationException("Модель поста ещё не загружена.");

        var content = PostContentType switch
        {
            DefaultPostContentTypes.BlockEditor => blockEditor1?.ContentJson ?? model.Content,
            DefaultPostContentTypes.Code => codeEditor1 is null ? model.Content : await codeEditor1.GetValue(),
            DefaultPostContentTypes.WYSIWYG => editor1 is null ? model.Content : await editor1.GetHTML(),
            _ => model.Content,
        };

        return JsonSerializer.Serialize(new
        {
            title = model.Title,
            slug = model.Slug,
            excerpt = model.Excerpt,
            tags = model.Tags,
            categories = model.CategoryIds,
            contentType = PostContentType,
            content,
            contentText = ExtractPlainText(content, PostContentType),
        }, AiJsonOptions);
    }

    public async Task<string> SetField(string field, string value)
    {
        var model = f.Model;

        switch (field.ToLowerInvariant())
        {
            case "title":
                model.Title = value;
                break;
            case "slug":
                model.Slug = value;
                break;
            case "excerpt":
                model.Excerpt = value;
                break;
            case "tags":
                model.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                break;
            case "categories":
                var ids = new List<Guid>();
                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(part, out var categoryId))
                        return $"Не удалось распознать Guid категории: '{part}'. Передайте Guid категорий через запятую.";
                    ids.Add(categoryId);
                }
                model.CategoryIds = [.. ids];
                break;
            case "content":
                var contentError = await SetContentValue(value);
                if (contentError is not null) return contentError;
                break;
            default:
                return $"Неизвестное поле '{field}'. Доступны: title, slug, excerpt, tags, categories, content.";
        }

        StateHasChanged();
        return $"Поле '{field}' изменено в форме (не сохранено).";
    }

    public async Task<string> Save()
    {
        try
        {
            var ok = await f.Save();
            return ok
                ? "Страница сохранена."
                : "Сохранение не выполнено: форма не прошла validation.";
        }
        catch (Exception ex)
        {
            return "Ошибка сохранения страницы: " + ex.GetBaseException().Message;
        }
    }

    private async Task<string?> SetContentValue(string value)
    {
        switch (PostContentType)
        {
            case DefaultPostContentTypes.BlockEditor:
                if (blockEditor1 is null) return "Редактор блоков ещё не инициализирован.";
                var json = BuildBlockEditorJson(value);
                blockEditor1.Content = EditorJsContent.FromJson(json);
                await blockEditor1.SetContent();
                f.Model.Content = json; // страховка, если JS onChange запаздывает
                return null;

            case DefaultPostContentTypes.Code:
                if (codeEditor1 is null) return "Редактор кода ещё не инициализирован.";
                await codeEditor1.SetValue(value);
                f.Model.Content = value;
                return null;

            case DefaultPostContentTypes.PlainText:
                f.Model.Content = value;
                return null;

            case DefaultPostContentTypes.WYSIWYG:
                return "Изменение WYSIWYG-контента агентом пока не поддерживается. Предложите пользователю отредактировать текст вручную.";

            default:
                f.Model.Content = value;
                return null;
        }
    }

    /// <summary>
    /// Собирает Editor.js JSON из обычного текста: каждая непустая строка — абзац.
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
        }, AiJsonOptions);
    }

    /// <summary>
    /// Plain-text извлечение контента для чтения моделью ИИ.
    /// </summary>
    static string ExtractPlainText(string content, string contentType)
    {
        if (string.IsNullOrEmpty(content)) return "";

        if (contentType == DefaultPostContentTypes.BlockEditor)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var sb = new StringBuilder();

                if (doc.RootElement.TryGetProperty("blocks", out var blocks))
                {
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
                }

                return sb.ToString().Trim();
            }
            catch
            {
                return content;
            }
        }

        if (contentType == DefaultPostContentTypes.WYSIWYG)
        {
            return Regex.Replace(content, "<[^>]+>", " ").Trim();
        }

        return content;
    }

    string PostContentType => f?.Model.PostType.PostContentSettings.PostContentType ?? "";

    async Task<BlockImage.ImageFileData?> OnImageFileRequest()
    {
        var mediaFile = await mediaService.OpenSelectMedia();

        if (mediaFile is null) return null;

        return new BlockImage.ImageFileData
        {
            Url = mediaFile.Url,
            FileName = mediaFile.Name,
            Size = (long)mediaFile.Size,
            //Width = mediaFile.Width,
            //Height = mediaFile.Height
        };
    }

    string blockEditorMenuButtonId = "blockEditorMenuButton-" + Guid.NewGuid().ToString();
    bool blockEditorMenuOpen;

    void BlockEditor_OnClickAISuggest()
    {
        aiTool.Open();
    }
}
