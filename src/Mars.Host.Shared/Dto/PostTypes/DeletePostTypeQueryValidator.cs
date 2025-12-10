using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostTypes;

public class DeletePostTypeQueryValidator : AbstractValidator<Guid>
{
    public DeletePostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x)
            .Custom((id, context) =>
            {
                var postType = metaModelTypesLocator.GetPostTypeById(id);

                if (postType == null)
                {
                    context.AddFailure($"post type '{postType.TypeName}' not exist");
                    return;
                }

                string[] undeletableTypes = ["post", "block", "page"];

                if (undeletableTypes.Contains(postType.TypeName))
                {
                    context.AddFailure($"post type '{postType.TypeName}' is internal type and cannot be delete");
                    return;
                }
            });
    }
}
