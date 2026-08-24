using FluentValidation;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;

namespace Mars.Host.Shared.Dto.Posts;

public class UpdatePostQueryValidator : AbstractValidator<UpdatePostQuery>
{
    public UpdatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator,
                                    IPostRepository postRepository,
                                    IPostCategoryRepository postCategoryRepository,
                                    IMetaValuesValidator metaValuesValidator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator, postCategoryRepository));

        RuleFor(x => x.MetaValues).ValidateMetaValues(metaValuesValidator, MetaValueOwnerCatalog.Post, x => x.Id);

        RuleFor(x => x.Id)
            .MustAsync(postRepository.ExistAsync)
            .WithMessage(x => $"Post Id '{x.Id}' not found");

    }
}
