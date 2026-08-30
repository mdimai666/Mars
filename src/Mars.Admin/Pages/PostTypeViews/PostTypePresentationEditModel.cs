using System.ComponentModel.DataAnnotations;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Resources;
using Mars.Core.Exceptions;
using Mars.Core.Interfaces;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Pages.PostTypeViews;

/// <summary>
/// <see cref="PostTypePresentationResponse"/>
/// </summary>
public class PostTypePresentationEditModel : IHasId
{
    [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
    public Guid Id { get; init; }

    [Display(Name = "ListViewTemplate", Description = "относительный путь шаблона во фронте админки, напр. postTypes/article/listView.hbs")]
    public string ListViewTemplate { get; set; } = "";

    /// <summary>Настройки колонок грида постов в админке; null — стандартный набор</summary>
    public PostTypeGridSettings? Grid { get; set; }

    public PostTypeSummaryResponse PostType { get; init; } = default!;

    public static async Task<PostTypePresentationEditModel> GetAction(IMarsWebApiClient client, Guid id)
    {
        var vm = await client.PostType.GetPresentationEditModel(id) ?? throw new NotFoundException();
        return ToModel(vm);
    }

    public static async Task<PostTypePresentationEditModel> SaveAction(IMarsWebApiClient client, PostTypePresentationEditModel postTypePresentation)
    {
        await client.PostType.UpdatePresentation(postTypePresentation.ToUpdateRequest());
        return postTypePresentation;
    }

    public UpdatePostTypePresentationRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            ListViewTemplate = ListViewTemplate,
            Grid = Grid,
        };

    public static PostTypePresentationEditModel ToModel(PostTypePresentationEditViewModel viewModel)
        => new()
        {
            Id = viewModel.PostType.Id,
            PostType = viewModel.PostType,

            ListViewTemplate = viewModel.Presentation.ListViewTemplate ?? "",
            Grid = viewModel.Presentation.Grid,
        };
}
