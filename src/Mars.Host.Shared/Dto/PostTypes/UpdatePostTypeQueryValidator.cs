using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostTypes;

public class UpdatePostTypeQueryValidator : AbstractValidator<UpdatePostTypeQuery>
{
    public UpdatePostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostTypeQueryValidator());

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var postType = metaModelTypesLocator.GetPostTypeByName(x.TypeName);

                if (postType is not null && postType.Id != x.Id)
                {
                    context.AddFailure(nameof(x.TypeName), $"Post type '{x.TypeName}' already exist");
                    return;
                }

            });
    }
}
