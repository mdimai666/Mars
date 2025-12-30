using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostTypes;

public class DeleteManyPostTypeQueryValidator : AbstractValidator<DeleteManyPostTypeQuery>
{
    public DeleteManyPostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("post ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeletePostTypeQueryValidator(metaModelTypesLocator));
    }
}
