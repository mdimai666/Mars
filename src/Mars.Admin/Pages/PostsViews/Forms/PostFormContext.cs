using Mars.Admin.Framework.Components.Forms;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Pages.PostsViews.Forms;

/// <summary>
/// Контекст формы поста: модель (значения живут в ней), хуки отложенной записи и живые редакторы
/// полей. Каскадируется в редакторы полей одним объектом — вместо россыпи отдельных каскадов.
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
}
