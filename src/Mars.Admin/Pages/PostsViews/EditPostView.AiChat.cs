using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars.AiChat.Front.Services;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Admin.Pages.PostsViews;

/// <summary>
/// Мост ИИ-агента (IAiChatPageHandler): чтение и правка полей формы поста через дескрипторы
/// общего слоя форм и стор значений. Системные слоты и метаполя идут одним путём: значение
/// парсится <see cref="FormValueText"/> по дескриптору и пишется в <see cref="Forms.PostFormValueStore"/>.
/// Тяжёлые редакторы (WYSIWYG, код, блочный) держат значение у себя: чтение — после коммита,
/// запись — через <see cref="Forms.FormLiveEditors"/>.
/// </summary>
public partial class EditPostView : IAiChatPageHandler
{
    private static readonly JsonSerializerOptions AiJsonOptions = new() { WriteIndented = false };

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

    /// <summary>Ключ редактора слота контента — из дескриптора формы (пусто = обычный многострочный текст)</summary>
    string ContentEditorKey => f?.Model.Form?.Field(SystemFieldsCatalog.Content)?.Field?.Editor ?? "";

    public string GetInfo()
    {
        var model = f?.Model;
        var descriptors = AiDescriptors(model);

        return JsonSerializer.Serialize(new
        {
            page = "EditPost",
            postType = PostTypeName,
            postId = model?.Id ?? ID,
            contentEditor = ContentEditorKey,
            imageFieldKey = model?.PostType.ImageFieldKey,
            postImageFeature = model?.PostType.FeatureActivated(PostTypeConstants.Features.PostImage) ?? false,
            fields = descriptors.Select(AiFieldInfo).ToArray(),
            editableFields = descriptors.Where(d => !d.ReadOnly).Select(d => d.Key).ToArray(),
            contentAiEditable = ContentEditorKey != FormEditorCatalog.Wysiwyg,
        }, AiJsonOptions);
    }

    public async Task<string> GetFields()
    {
        await _commits.CommitAllAsync();

        var model = f?.Model ?? throw new InvalidOperationException("Модель поста ещё не загружена.");
        var content = model.Content;
        var values = FormContextOf(model).Values;

        // метаполя — отдельным объектом: ключ метаполя теоретически может совпасть с ключом слота
        var meta = new Dictionary<string, object?>();
        foreach (var descriptor in AiDescriptors(model))
        {
            if (SystemFieldsCatalog.Find(descriptor.Key) is not null) continue;
            meta[descriptor.Key] = AiValue(IsListField(descriptor) ? values.GetList(descriptor) : values.GetValue(descriptor));
        }

        return JsonSerializer.Serialize(new
        {
            title = model.Title,
            slug = model.Slug,
            excerpt = model.Excerpt,
            tags = model.Tags,
            categories = model.CategoryIds,
            status = model.Status,
            lang = model.LangCode,
            contentEditor = ContentEditorKey,
            content,
            contentText = ExtractPlainText(content, ContentEditorKey),
            meta,
        }, AiJsonOptions);
    }

    public async Task<string> SetField(string field, string value)
    {
        var model = f?.Model;
        if (model is null) return "Модель поста ещё не загружена.";

        var descriptor = model.Form?.Field(field)?.Field;
        if (descriptor is null)
        {
            var available = string.Join(", ", model.Form?.Fields().Select(i => i.Key) ?? []);
            return $"Неизвестное поле '{field}'. Доступны: {available}.";
        }

        if (descriptor.ReadOnly)
            return $"Поле '{field}' только для чтения.";

        // тяжёлый редактор держит значение у себя — пишем в экземпляр редактора, в форму оно уйдёт перед сохранением
        if (_liveEditors.Find(descriptor.Key) is { } liveEditor)
        {
            var editorError = await liveEditor(value);
            if (editorError is not null) return editorError;

            StateHasChanged();
            return FieldChangedMessage(field);
        }

        if (descriptor.Editor == FormEditorCatalog.Wysiwyg)
            return $"Запись в поле '{field}' (WYSIWYG) агентом пока не поддерживается. Предложите пользователю отредактировать его вручную.";

        if (IsHeavyEditor(descriptor.Editor))
            return $"Редактор поля '{field}' ещё не инициализирован — повторите попытку чуть позже.";

        if (!FormValueText.TryToClr(value, descriptor, out var parsed, out var parseError))
            return parseError!;

        var store = FormContextOf(model).Values;
        if (IsListField(descriptor))
            store.SetList(descriptor, parsed as IEnumerable<object?> ?? []);
        else
            store.SetValue(descriptor, parsed);

        StateHasChanged();
        return FieldChangedMessage(field);
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

    //=====================================

    static string FieldChangedMessage(string field) => $"Поле '{field}' изменено в форме (не сохранено).";

    static bool IsListField(FormFieldDescriptor descriptor)
        => descriptor.Multiple || descriptor.Type == FormFieldType.SelectMany;

    static bool IsHeavyEditor(string? editor)
        => editor is FormEditorCatalog.Code or FormEditorCatalog.BlockEditor;

    /// <summary>Дескрипторы полей формы в порядке раскладки (метаполя + системные слоты)</summary>
    static IReadOnlyList<FormFieldDescriptor> AiDescriptors(PostEditModel? model)
        => model?.Form?.Fields().Select(item => item.Field!).ToList() ?? [];

    object AiFieldInfo(FormFieldDescriptor descriptor) => new
    {
        key = descriptor.Key,
        title = descriptor.TitleKey is { Length: > 0 } titleKey ? ResolveTitle(titleKey) : descriptor.Title,
        type = descriptor.Type.ToString(),
        multiple = descriptor.Multiple,
        required = descriptor.Required,
        readOnly = descriptor.ReadOnly,
        editor = descriptor.Editor,
        modelName = descriptor.ModelName,
        variants = descriptor.Choices.Count > 0 ? descriptor.Choices.Select(c => c.Key).ToArray() : null,
        system = SystemFieldsCatalog.Find(descriptor.Key) is not null,
    };

    /// <summary>Каноническое значение стора → JSON для модели: Guid и даты — строками, пустая ссылка — null</summary>
    static object? AiValue(object? value) => value switch
    {
        null => null,
        string text => text,
        Guid id => id == Guid.Empty ? null : id.ToString("D"),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTime date => new DateTimeOffset(date).ToString("O", CultureInfo.InvariantCulture),
        System.Collections.IEnumerable sequence => sequence.Cast<object?>().Select(AiValue).ToArray(),
        _ => value,
    };

    /// <summary>
    /// Plain-text извлечение контента для чтения моделью ИИ.
    /// </summary>
    static string ExtractPlainText(string content, string contentEditor)
    {
        if (string.IsNullOrEmpty(content)) return "";

        if (contentEditor == FormEditorCatalog.BlockEditor)
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

        if (contentEditor == FormEditorCatalog.Wysiwyg)
        {
            return Regex.Replace(content, "<[^>]+>", " ").Trim();
        }

        return content;
    }
}
