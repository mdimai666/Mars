using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Pages.PostsViews.Forms;

/// <summary>
/// Значения формы поста живут в самой модели: стор читает и пишет её свойства напрямую
/// (системные слоты — типизированные свойства, метаполя — строки <see cref="PostEditModel.MetaValues"/>).
/// Отдельного мешка значений у формы поста нет — сохранение отправляет модель как есть.
/// </summary>
public sealed class PostFormValueStore(PostEditModel post) : IFormValueStore
{
    public IReadOnlyCollection<FormError> Errors { get; set; } = [];

    public event Action? Changed;

    public object? GetValue(FormFieldDescriptor field)
    {
        if (IsList(field)) return GetList(field);
        if (IsMeta(field)) return Row(field);

        return field.Key switch
        {
            SystemFieldsCatalog.Title => post.Title,
            SystemFieldsCatalog.Slug => post.Slug,
            SystemFieldsCatalog.Excerpt => post.Excerpt,
            SystemFieldsCatalog.Status => post.Status,
            SystemFieldsCatalog.Lang => post.LangCode,
            SystemFieldsCatalog.CreatedAt => post.CreatedAt,
            SystemFieldsCatalog.ModifiedAt => post.ModifiedAt,
            // автор только для чтения: показываем подпись, пикера пользователя нет
            SystemFieldsCatalog.Author => AuthorName(),
            _ => null,
        };
    }

    public IReadOnlyList<object?> GetList(FormFieldDescriptor field)
    {
        if (IsMeta(field))
        {
            return post.MetaValues.Where(v => v.MetaField.Key == field.Key)
                                  .OrderBy(v => v.Index)
                                  .Cast<object?>()
                                  .ToList();
        }

        return field.Key switch
        {
            SystemFieldsCatalog.Tags => post.Tags.Cast<object?>().ToList(),
            SystemFieldsCatalog.Categories => post.CategoryIds.Cast<object?>().ToList(),
            _ => [],
        };
    }

    public void SetValue(FormFieldDescriptor field, object? value)
    {
        // строка метаполя правится на месте — записывать нечего
        if (IsMeta(field)) return;

        switch (field.Key)
        {
            case SystemFieldsCatalog.Title:
                var title = value as string ?? "";
                if (post.Title == title) return;
                post.Title = title;
                post.AutoFillSlug();
                break;
            case SystemFieldsCatalog.Slug:
                post.Slug = value as string ?? "";
                break;
            case SystemFieldsCatalog.Excerpt:
                post.Excerpt = value as string ?? "";
                break;
            case SystemFieldsCatalog.Status:
                post.Status = value as string ?? "";
                break;
            case SystemFieldsCatalog.Lang:
                post.LangCode = value as string ?? "";
                break;
            case SystemFieldsCatalog.CreatedAt when value is DateTimeOffset created:
                post.CreatedAt = created;
                break;
            default:
                return;
        }

        Changed?.Invoke();
    }

    public void SetList(FormFieldDescriptor field, IEnumerable<object?> values)
    {
        var items = values.ToList();

        if (IsMeta(field))
        {
            var rows = items.OfType<MetaValueEditModel>().ToList();
            for (var i = 0; i < rows.Count; i++) rows[i].Index = i;

            post.MetaValues.RemoveAll(v => v.MetaField.Key == field.Key);
            post.MetaValues.AddRange(rows);
        }
        else if (field.Key == SystemFieldsCatalog.Tags)
        {
            post.Tags = items.OfType<string>().ToArray();
        }
        else if (field.Key == SystemFieldsCatalog.Categories)
        {
            post.CategoryIds = items.OfType<Guid>().ToArray();
        }
        else
        {
            return;
        }

        Changed?.Invoke();
    }

    /// <summary>Носитель значения — строки владельца: доменные редакторы правят их напрямую</summary>
    public object? NativeValue(FormFieldDescriptor field) => post.MetaValues;

    string AuthorName()
    {
        var author = post.Author;
        return string.IsNullOrWhiteSpace(author?.DisplayName)
            ? author?.UserName ?? "—"
            : author!.DisplayName!;
    }

    static bool IsList(FormFieldDescriptor field)
        => field.Multiple || field.Type == FormFieldType.SelectMany;

    bool IsMeta(FormFieldDescriptor field)
        => post.MetaValues.Any(v => v.MetaField.Key == field.Key)
           || post.PostType.MetaFields.Any(f => f.Key == field.Key);

    MetaValueEditModel? Row(FormFieldDescriptor field)
        => post.MetaValues.FirstOrDefault(v => v.MetaField.Key == field.Key && v.Index == 0);
}
