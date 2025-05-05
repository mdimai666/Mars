using Mars.Host.Shared.Services;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Posts;

public class UpdatePostQueryValidator : AbstractValidator<UpdatePostQuery>
{
    public UpdatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator));

        //RuleFor(x => x.Title)
        //    .Equal("zuzu");

        //RuleFor(x => x)
        //    .CustomAsync(async (query, context, cancellationToken) =>
        //    {
        //        metaModelTypesLocator.MetaMtoModelsCompiledTypeDict
        //    });
    }
}
