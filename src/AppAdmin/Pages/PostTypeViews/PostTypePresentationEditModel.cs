using System.ComponentModel.DataAnnotations;
using Mars.Core.Exceptions;
using Mars.Core.Interfaces;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Resources;
using Mars.Shared.Validators;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.PostTypeViews;

/// <summary>
/// <see cref="PostTypePresentationResponse"/>
/// </summary>
public class PostTypePresentationEditModel : IHasId
{
    [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
    public Guid Id { get; init; }

    [ValidateSourceUri]
    [Display(Name = "ListViewTemplate", Description = "путь к странице отрисовки")]
    public string ListViewTemplate { get; set; } = "";

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
            ListViewTemplate = ListViewTemplate.ToString(),
        };

    public static PostTypePresentationEditModel ToModel(PostTypePresentationEditViewModel viewModel)
        => new()
        {
            Id = viewModel.PostType.Id,
            PostType = viewModel.PostType,

            ListViewTemplate = viewModel.Presentation.ListViewTemplate ?? ""
        };
}
