using FluentValidation;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Core.Extensions;

namespace Mars.Cms.Abstractions.Dto.PostJsons;

public class CreatePostJsonQueryValidator : AbstractValidator<CreatePostJsonQuery>
{
    public CreatePostJsonQueryValidator(IMetaModelTypesLocator metaModelTypesLocator,
                                        IMetaValuesValidator metaValuesValidator)
    {
        RuleFor(x => x)
            .CustomAsync(async (x, context, cancellationToken) =>
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

                var ownerContext = new MetaValueValidationContext { ModelName = MetaValueOwnerCatalog.Post, OwnerId = x.Id };
                foreach (var error in await metaValuesValidator.ValidateJsonAsync(postType.MetaFields, x.Meta, requireAll: true, ownerContext, postType.ContentField()?.Key, cancellationToken))
                    context.AddFailure(nameof(x.Meta), $"поле '{error.FieldKey}': {error.Message}");
            });

    }
}
