using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Pages.PostsViews.Forms;

/// <summary>
/// Контекст формы поста: модель (значения живут в ней), хуки отложенной записи и живые редакторы
/// полей. Каскадируется в редакторы полей одним объектом — вместо россыпи отдельных каскадов
/// (модель, значения, метаполя, реестр редакторов, holder).
/// </summary>
public sealed class PostFormContext(PostEditModel post,
                                    IFormValueStore values,
                                    FormLiveEditors liveEditors,
                                    FormCommitHooks commits)
{
    public PostEditModel Post { get; } = post;

    public IFormValueStore Values { get; } = values;

    public FormLiveEditors LiveEditors { get; } = liveEditors;

    public FormCommitHooks Commits { get; } = commits;

    /// <summary>Дерево формы; подменяется на месте после сохранения раскладки типа — читается каждый рендер</summary>
    public FormDefinition Definition => Post.Form ?? new FormDefinition { OwnerModel = $"post.{Post.Type}" };

    MetaValueContext? _meta;
    List<MetaValueEditModel>? _metaValues;
    List<MetaFieldEditModel>? _metaFields;

    /// <summary>Мета-контекст (значения и определения полей) — общий контракт с формами пользователей и категорий</summary>
    public MetaValueContext Meta
    {
        get
        {
            if (_meta is null
                || !ReferenceEquals(_metaValues, Post.MetaValues)
                || !ReferenceEquals(_metaFields, Post.PostType.MetaFields))
            {
                _metaValues = Post.MetaValues;
                _metaFields = Post.PostType.MetaFields;
                _meta = new MetaValueContext { Values = _metaValues, Fields = _metaFields };
            }

            return _meta;
        }
    }
}
