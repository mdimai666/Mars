using Mars.Core.Extensions;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using MarsCodeEditor2;
using MarsEditors;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews;

public partial class EditPostView
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ID { get; set; }
    [Parameter, EditorRequired] public string PostTypeName { get; set; } = default!;

    StandartEditContainer<PostEditModel> f = default!;

    //OLD
    WysiwygEditor? editor1;

    // MarsCodeEditor? codeEditor1;
    CodeEditor2? codeEditor1;

    string lang1 = MarsCodeEditor.Language.handlebars;


    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(f.Model.Slug) || Guid.TryParse(f.Model.Slug, out Guid _))
        {
            f.Model.Slug = Tools.TranslateToPostSlug(f.Model.Title);
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
            else if (contentType == PostTypeConstants.DefaultPostContentTypes.WYSIWYG)
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
        codeEditor1?.Dispose();
    }

    string PostContentType => f?.Model.PostType.PostContentSettings.PostContentType??"";
}
