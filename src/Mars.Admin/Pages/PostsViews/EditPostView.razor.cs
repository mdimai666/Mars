using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.Pages.PostsViews.Forms;
using Mars.AiChat.Front.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Front;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.PostsViews;

public partial class EditPostView : IAiChatPageHandler
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
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

    StandardEditContainer<PostEditModel> f = default!;

    //==========================================
    // Форма: дерево контейнеров (Mars.Forms)

    /// <summary>Реестр тяжёлых редакторов мета-значений — один на все зоны формы</summary>
    readonly IHeavyMetaValueEditors _heavyEditors = new HeavyMetaValueEditorRegistry();

    /// <summary>Доступ к редактору контента, который рендерится внутри дерева</summary>
    readonly PostContentEditorHolder _contentHolder = new();

    FormValuesModel? _formValues;
    PostEditModel? _formValuesOwner;

    /// <summary>
    /// Мешок значений системных слотов: пересоздаётся вместе с моделью, каждое изменение сразу
    /// уходит в типизированную модель и возвращается обратно (авто-подстановка slug, внешние правки).
    /// </summary>
    FormValuesModel FormValuesOf(PostEditModel model)
    {
        if (_formValues is not null && ReferenceEquals(_formValuesOwner, model)) return _formValues;

        var values = new FormValuesModel(model.BuildFormValues(model.Form?.OwnerModel ?? $"post.{model.Type}"));
        values.Changed += () =>
        {
            model.ApplyFormValues(values.Values);
            model.FillFormValues(values.Values);
            StateHasChanged();
        };

        _formValues = values;
        _formValuesOwner = model;
        _contentHolder.RequestSave = () => f.OnSubmit();

        return values;
    }

    /// <summary>Обновить мешок после внешнего изменения модели (инструменты ИИ-агента)</summary>
    void RefreshFormValues(PostEditModel model)
    {
        if (_formValues is not null && ReferenceEquals(_formValuesOwner, model))
            model.FillFormValues(_formValues.Values);
    }

    /// <summary>Заголовки системных слотов — ключи ресурса <see cref="AppRes"/></summary>
    string ResolveTitle(string key) => L[key];

    async Task<PostEditModel> SaveWithCallback(PostEditModel post, bool isNew)
    {
        var result = await PostEditModel.SaveAction(client, post, isNew);
        if (OnSaved.HasDelegate) await OnSaved.InvokeAsync(result);
        return result;
    }

    async Task BeforeSave(PostEditModel post)
    {
        await _heavyEditors.PullAsync();

        if (_contentHolder.Current is { } content)
            post.Content = await content.GetContentAsync();
    }

    public void Dispose()
    {
        if (ReferenceEquals(AiChatPageHandlerHolder.Current, this))
            AiChatPageHandlerHolder.Current = null;
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

    /// <summary>Поля, которые агент умеет менять; остальные — только чтение через <see cref="GetFields"/></summary>
    static readonly string[] AgentEditableFields =
    [
        SystemFieldsCatalog.Title, SystemFieldsCatalog.Slug, SystemFieldsCatalog.Excerpt,
        SystemFieldsCatalog.Tags, SystemFieldsCatalog.Categories, FeatureFieldsCatalog.ContentFieldKey,
    ];

    string ContentEditorKey => f?.Model.PostType.ContentEditorKey() ?? "";

    public string GetInfo()
    {
        var formKeys = f?.Model?.Form?.Fields().Select(i => i.Key).ToArray() ?? [];

        return JsonSerializer.Serialize(new
        {
            page = "EditPost",
            postType = PostTypeName,
            postId = f?.Model?.Id ?? ID,
            contentEditor = ContentEditorKey,
            fields = formKeys,
            editableFields = AgentEditableFields.Where(formKeys.Contains).ToArray(),
            contentAiEditable = ContentEditorKey != MetaFieldEditorCatalog.Wysiwyg,
        }, AiJsonOptions);
    }

    public async Task<string> GetFields()
    {
        await _heavyEditors.PullAsync();

        var model = f?.Model ?? throw new InvalidOperationException("Модель поста ещё не загружена.");
        var content = _contentHolder.Current is { } editor ? await editor.GetContentAsync() : model.Content;

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
            case SystemFieldsCatalog.Title:
                model.Title = value;
                break;
            case SystemFieldsCatalog.Slug:
                model.Slug = value;
                break;
            case SystemFieldsCatalog.Excerpt:
                model.Excerpt = value;
                break;
            case SystemFieldsCatalog.Tags:
                model.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                break;
            case SystemFieldsCatalog.Categories:
                var ids = new List<Guid>();
                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(part, out var categoryId))
                        return $"Не удалось распознать Guid категории: '{part}'. Передайте Guid категорий через запятую.";
                    ids.Add(categoryId);
                }
                model.CategoryIds = [.. ids];
                break;
            case FeatureFieldsCatalog.ContentFieldKey:
                if (_contentHolder.Current is not { } editor) return "Редактор контента ещё не инициализирован.";

                var contentError = await editor.TrySetContentAsync(value);
                if (contentError is not null) return contentError;
                break;
            default:
                return $"Неизвестное поле '{field}'. Доступны: {string.Join(", ", AgentEditableFields)}.";
        }

        model.AutoFillSlug();
        RefreshFormValues(model);
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
                        var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
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
}
