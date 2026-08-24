using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppFront.Shared.Components.MetaFieldViews;
using EditorJsBlazored;
using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;
using Mars.AiChat.Front.Services;
using Mars.Core.Features;
using Mars.Shared.Contracts.MetaFields;
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

    /// <summary>Вызывается после каждого сохранения поста (например, дровером секции детей)</summary>
    [Parameter] public EventCallback<PostEditModel> OnSaved { get; set; }

    /// <summary>Переход на URL созданной записи после сохранения (false в боковой панели)</summary>
    [Parameter] public bool NavigateAfterCreate { get; set; } = true;

    [Parameter] public bool HidePublishCard { get; set; }

    StandartEditContainer<PostEditModel> f = default!;

    async Task<PostEditModel> SaveWithCallback(PostEditModel post, bool isNew)
    {
        var result = await PostEditModel.SaveAction(client, post, isNew);
        if (OnSaved.HasDelegate) await OnSaved.InvokeAsync(result);
        return result;
    }

    //OLD
    WysiwygEditor? editor1;
    CodeEditor2? codeEditor1;
    BlockEditor1? blockEditor1;

    FormMetaValue? metaValueForm;

    string lang1 = "";

    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(f.Model.Slug) || Guid.TryParse(f.Model.Slug, out Guid _))
        {
            f.Model.Slug = TextTool.TranslateToPostSlug(f.Model.Title);
        }
    }

    async Task BeforeSave(PostEditModel post)
    {
        if (metaValueForm is not null) await metaValueForm.PullAsync();

        if (post.FeatureActivated(PostTypeConstants.Features.Content))
        {
            var editorKey = post.PostType.ContentEditorKey();

            if (editorKey == MetaFieldEditorCatalog.Wysiwyg)
            {

                if (editor1 is not null)
                {
                    post.Content = await editor1!.GetHTML();
                }
            }
            else if (editorKey == MetaFieldEditorCatalog.Code)
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
            contentEditor = ContentEditorKey,
            fields = new[] { "title", "slug", "excerpt", "tags", "categories", "content" },
            contentAiEditable = ContentEditorKey != MetaFieldEditorCatalog.Wysiwyg,
        }, AiJsonOptions);
    }

    public async Task<string> GetFields()
    {
        if (metaValueForm is not null) await metaValueForm.PullAsync();

        var model = f?.Model ?? throw new InvalidOperationException("Модель поста ещё не загружена.");

        var content = ContentEditorKey switch
        {
            MetaFieldEditorCatalog.BlockEditor => blockEditor1?.ContentJson ?? model.Content,
            MetaFieldEditorCatalog.Code => codeEditor1 is null ? model.Content : await codeEditor1.GetValue(),
            MetaFieldEditorCatalog.Wysiwyg => editor1 is null ? model.Content : await editor1.GetHTML(),
            _ => model.Content,
        };

        return JsonSerializer.Serialize(new
        {
            title = model.Title,
            slug = model.Slug,
            excerpt = model.Excerpt,
            tags = model.Tags,
            categories = model.CategoryIds,
            contentEditor = ContentEditorKey,
            content,
            contentText = ExtractPlainText(content, ContentEditorKey),
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
        switch (ContentEditorKey)
        {
            case var k when k == MetaFieldEditorCatalog.BlockEditor:
                if (blockEditor1 is null) return "Редактор блоков ещё не инициализирован.";
                var json = BuildBlockEditorJson(value);
                blockEditor1.Content = EditorJsContent.FromJson(json);
                await blockEditor1.SetContent();
                f.Model.Content = json; // страховка, если JS onChange запаздывает
                return null;

            case var k when k == MetaFieldEditorCatalog.Code:
                if (codeEditor1 is null) return "Редактор кода ещё не инициализирован.";
                await codeEditor1.SetValue(value);
                f.Model.Content = value;
                return null;

            case var k when k == MetaFieldEditorCatalog.Wysiwyg:
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
    static string ExtractPlainText(string content, string contentEditor)
    {
        if (string.IsNullOrEmpty(content)) return "";

        if (contentEditor == MetaFieldEditorCatalog.BlockEditor)
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

        if (contentEditor == MetaFieldEditorCatalog.Wysiwyg)
        {
            return Regex.Replace(content, "<[^>]+>", " ").Trim();
        }

        return content;
    }

    string ContentEditorKey => f?.Model.PostType.ContentEditorKey() ?? "";

    string ContentCodeLang => f?.Model.PostType.ContentCodeLang() ?? MetaFieldEditorCatalog.DefaultCodeLang;

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
