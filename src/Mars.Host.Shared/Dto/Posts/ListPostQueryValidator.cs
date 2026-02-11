using FluentValidation;
using Mars.Core.Constants;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Dto.Posts;

public class ListPostQueryValidator : AbstractValidator<ListPostQuery>
{
    static readonly HashSet<string> AllowedSortColumns = new(comparer: StringComparer.OrdinalIgnoreCase) {
        nameof(PostListItemResponse.Id),
        nameof(PostListItemResponse.CreatedAt),
        nameof(PostListItemResponse.Slug),
        nameof(PostListItemResponse.Title),
        nameof(PostListItemResponse.Status),
        nameof(PostListItemResponse.Author),
        nameof(PostListItemResponse.Categories),
        nameof(PostListItemResponse.Tags),
        };

    public ListPostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.Type)
            .Must(v => v == null || metaModelTypesLocator.GetPostTypeByName(v) != null)
            .WithErrorCode(nameof(HttpConstants.UserActionErrorCode466))
            .WithMessage(v => $"post type '{v.Type}' not exist");

        RuleFor(x => x.Sort)
            .Must(v => AllowedSortColumns.Contains(v.TrimStart('-')))
            .When(x => x.Sort.IsNotNullOrEmpty())
            .WithMessage(x => $"Sorting by field '{x.Sort}' not support");
    }
}
