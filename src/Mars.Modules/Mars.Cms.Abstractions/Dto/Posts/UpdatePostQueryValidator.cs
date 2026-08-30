using FluentValidation;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Validators;

namespace Mars.Cms.Abstractions.Dto.Posts;

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
