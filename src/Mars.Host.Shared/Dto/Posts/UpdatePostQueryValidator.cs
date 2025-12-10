using Mars.Host.Shared.Services;
using FluentValidation;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Posts;

public class UpdatePostQueryValidator : AbstractValidator<UpdatePostQuery>
{
    public UpdatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator, IPostRepository postRepository)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator));

        RuleFor(x => x.Id)
            .MustAsync(postRepository.ExistAsync)
            .WithMessage(x => $"Post Id '{x.Id}' not found");

        //RuleFor(x => x.Title)
        //    .Equal("zuzu");

        //RuleFor(x => x)
        //    .CustomAsync(async (query, context, cancellationToken) =>
        //    {
        //        metaModelTypesLocator.MetaMtoModelsCompiledTypeDict
        //    });
    }
}
