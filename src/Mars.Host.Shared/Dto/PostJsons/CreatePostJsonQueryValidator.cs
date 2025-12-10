using FluentValidation;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostJsons;

public class CreatePostJsonQueryValidator : AbstractValidator<CreatePostJsonQuery>
{
    public CreatePostJsonQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var postType = metaModelTypesLocator.GetPostTypeByName(x.Type);

                if (postType == null)
                {
                    context.AddFailure(nameof(x.Type), $"post type '{x.Type}' not exist");
                    return;
                }

                if (postType.Disabled)
                {
                    context.AddFailure(nameof(x.Type), $"post type '{x.Type}' is disabled");
                    return;
                }

                if (x.Meta is not null)
                {
                    var keys = x.Meta.Keys;
                    var validMetaKeys = postType.MetaFields.Select(s => s.Key).ToList();

                    var invalidKeys = keys.Except(validMetaKeys);

                    if (invalidKeys.Any())
                    {
                        context.AddFailure(nameof(x.Meta), $"meta keys'{invalidKeys.JoinStr(",")}' not exist");
                        return;
                    }
                }
            });

    }
}
