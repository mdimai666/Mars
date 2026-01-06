using FluentValidation;
using Mars.Core.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Models;

namespace Mars.Host.Shared.Dto.PostTypes;

public record UpdatePostTypePresentationQuery
{
    public required Guid Id { get; init; }
    public required SourceUri ListViewTemplate { get; init; }

}

public class UpdatePostTypePresentationQueryValidator : AbstractValidator<UpdatePostTypePresentationQuery>
{
    public UpdatePostTypePresentationQueryValidator(IPostRepository postRepository)
    {
        RuleFor(x => x)
            .CustomAsync(async (x, context, ct) =>
            {
                if (!x.ListViewTemplate.ToString().IsNullOrEmpty())
                {
                    if (x.ListViewTemplate.SegmentsCount != 2)
                    {
                        context.AddFailure(nameof(x.ListViewTemplate), $"SourceUri '{x.ListViewTemplate}' required 2 segments");
                        return;
                    }

                    var postTypeName = x.ListViewTemplate[0];
                    var postSlug = x.ListViewTemplate[1];

                    if (!await postRepository.ExistAsync(postTypeName, postSlug, ct))
                    {
                        context.AddFailure(nameof(x.ListViewTemplate), $"post uri '{x.ListViewTemplate}' not found");
                        return;
                    }
                }

            });
    }
}
