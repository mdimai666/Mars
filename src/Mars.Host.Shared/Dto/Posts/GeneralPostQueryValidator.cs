using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.PostTypes;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Posts;

public class GeneralPostQueryValidator : AbstractValidator<IGeneralPostQuery>
{

    public GeneralPostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Must(Tools.IsValidSlugWithUpperCase)
            .WithMessage(v => $"'{v.Slug}' is Invalid slug");

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                //var postType = await postTypeRepository.GetDetailByName(x.Type, ct);
                var postType = metaModelTypesLocator.GetPostTypeByName(x.Type);

                if (postType == null)
                {
                    context.AddFailure(nameof(x.Type), $"post type '{x.Type}' not exist");
                    return;
                }

                if (postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status))
                {
                    if (!postType.PostStatusList.Any(s => s.Slug == x.Status))
                    {
                        context.AddFailure(nameof(x.Status), $"status '{x.Status}' not exist");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(x.Status))
                    {
                        context.AddFailure(nameof(x.Status), $"status feature is disabled. status must be empty");
                    }
                }
            });

    }
}
