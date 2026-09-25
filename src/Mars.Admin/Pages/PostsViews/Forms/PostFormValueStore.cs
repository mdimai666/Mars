using System.Collections;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Pages.PostsViews.Forms;

/// <summary>
/// Значения формы поста живут в самой модели: системные слоты — типизированные свойства, метаполя —
/// строки <see cref="PostEditModel.MetaValues"/>. Отдельного мешка значений у формы поста нет —
/// сохранение отправляет модель как есть.
/// </summary>
public sealed class PostFormValueStore : IFormValueStore
{
    readonly PostEditModel _post;
    readonly MetaValueStore _meta;

    public PostFormValueStore(PostEditModel post)
    {
        _post = post;
        _meta = new MetaValueStore(post.MetaValues, post.PostType.MetaFields);
        _meta.Changed += () => Changed?.Invoke();
    }

    public IReadOnlyCollection<FormError> Errors { get; set; } = [];

    public event Action? Changed;

    public object? GetValue(FormFieldDescriptor field)
    {
        if (IsMeta(field)) return _meta.GetValue(field);
        if (IsList(field)) return GetList(field);

        return field.Key switch
        {
            SystemFieldsCatalog.Title => _post.Title,
            SystemFieldsCatalog.Slug => _post.Slug,
            SystemFieldsCatalog.Content => _post.Content,
            SystemFieldsCatalog.Excerpt => _post.Excerpt,
            SystemFieldsCatalog.Status => _post.Status,
            SystemFieldsCatalog.Lang => _post.LangCode,
            SystemFieldsCatalog.CreatedAt => _post.CreatedAt,
            SystemFieldsCatalog.ModifiedAt => _post.ModifiedAt,
            // автор только для чтения: показываем подпись, пикера пользователя нет
            SystemFieldsCatalog.Author => AuthorName(),
            _ => null,
        };
    }

    public IReadOnlyList<object?> GetList(FormFieldDescriptor field)
    {
        if (IsMeta(field)) return _meta.GetList(field);

        return field.Key switch
        {
            SystemFieldsCatalog.Tags => _post.Tags.Cast<object?>().ToList(),
            SystemFieldsCatalog.Categories => _post.CategoryIds.Cast<object?>().ToList(),
            _ => [],
        };
    }

    public void SetValue(FormFieldDescriptor field, object? value)
    {
        if (IsMeta(field))
        {
            _meta.SetValue(field, value);
            return;
        }

        // множественный слот: редактор отдаёт значение целиком списком (теги, категории)
        if (IsList(field))
        {
            SetList(field, value is IEnumerable items ? items.Cast<object?>() : []);
            return;
        }

        switch (field.Key)
        {
            case SystemFieldsCatalog.Title:
                var title = value as string ?? "";
                if (_post.Title == title) return;
                _post.Title = title;
                _post.AutoFillSlug();
                break;
            case SystemFieldsCatalog.Slug:
                _post.Slug = value as string ?? "";
                break;
            case SystemFieldsCatalog.Content:
                _post.Content = value as string ?? "";
                break;
            case SystemFieldsCatalog.Excerpt:
                _post.Excerpt = value as string ?? "";
                break;
            case SystemFieldsCatalog.Status:
                _post.Status = value as string ?? "";
                break;
            case SystemFieldsCatalog.Lang:
                _post.LangCode = value as string ?? "";
                break;
            case SystemFieldsCatalog.CreatedAt when value is DateTimeOffset created:
                _post.CreatedAt = created;
                break;
            default:
                return;
        }

        Changed?.Invoke();
    }

    public void SetList(FormFieldDescriptor field, IEnumerable<object?> values)
    {
        if (IsMeta(field))
        {
            _meta.SetList(field, values);
            return;
        }

        var items = values.ToList();

        if (field.Key == SystemFieldsCatalog.Tags)
        {
            _post.Tags = items.OfType<string>().ToArray();
        }
        else if (field.Key == SystemFieldsCatalog.Categories)
        {
            _post.CategoryIds = items.OfType<Guid>().ToArray();
        }
        else
        {
            return;
        }

        Changed?.Invoke();
    }

    string AuthorName()
    {
        var author = _post.Author;
        return string.IsNullOrWhiteSpace(author?.DisplayName)
            ? author?.UserName ?? "—"
            : author!.DisplayName!;
    }

    static bool IsList(FormFieldDescriptor field)
        => field.Multiple || field.Type == FormFieldType.SelectMany;

    bool IsMeta(FormFieldDescriptor field)
        => _post.MetaValues.Any(value => value.MetaField.Key == field.Key)
           || _post.PostType.MetaFields.Any(meta => meta.Key == field.Key);
}
