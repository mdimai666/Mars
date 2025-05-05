using AppFront.Shared.Interfaces;
using AppFront.Shared.Services.GallerySpace;
using AppShared.Dto.Gallery;
using AppShared.Models;
using MarsCodeEditor2;
using Mars.Core.Extensions;
using MarsEditors;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews.SpecialViews;

public partial class GalleryEditView
{
    [Inject] MediaService mediaService { get; set; } = default!;
    [Inject] GalleryService galleryService { get; set; } = default!;
    [Inject] IMessageService messageService { get; set; } = default!;


    [Parameter] public Guid ID { get; set; }
    StandartEditContainer<Post, PostService> f = default!;

    [Parameter]
    public string QueryPostType { get; set; } = "gallery";

    PostType postType = new();

    WysiwygEditor? editor1;

    // MarsCodeEditor? codeEditor1;
    CodeEditor2? codeEditor1;

    string lang1 = MarsCodeEditor.Language.handlebars;

    GalleryPhotosList galleryPhotosList = default!;

    protected override void OnInitialized()
    {

        base.OnInitialized();

        if (ID == Guid.Empty)
        {
            postType = Q.Site.PostTypes.First(s => s.TypeName == QueryPostType);

            if (postType.EnabledFeatures.Contains(nameof(Post.Content)) && postType.PostContentType == EPostContentType.Code)
            {
                //_ = DotnetKeepMe();
            }
        }
        //after load model

    }

    void OnLoadData(Post post)
    {
        postType = Q.Site.PostTypes.First(s => s.TypeName == post.Type);

        galleryPhotosList.Refresh();
    }


    void OnChangeTitle()
    {
        if (string.IsNullOrWhiteSpace(f.Model.Slug) || Guid.TryParse(f.Model.Slug, out Guid _))
        {
            f.Model.Slug = Tools.TranslateToPostSlug(f.Model.Title);
        }
    }

    async Task BeforeSave(Post post)
    {
        if (postType.EnabledFeatures.Contains(nameof(Post.Content)))
        {
            if (postType.PostContentType == EPostContentType.WYSIWYG)
            {

                if (editor1 is not null)
                {
                    post.Content = await editor1!.GetHTML();
                }
            }
            else if (postType.PostContentType == EPostContentType.Code)
            {
                post.Content = await codeEditor1!.GetValue();

            }
        }
        f.Model.Type = postType.TypeName;

    }

    void OnSaveFromCodeEditor(string value)
    {
        f.Model.Content = value;
        _ = f.OnFinish();
    }

    public void Dispose()
    {
        codeEditor1?.Dispose();
    }

}

//public class PhotoPost : PostDto
//{
//    public FileEntity? image { get; set; }

//}
