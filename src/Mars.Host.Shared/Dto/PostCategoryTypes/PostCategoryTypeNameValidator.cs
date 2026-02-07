using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public class PostCategoryTypeNameValidator
{
    public PostCategoryTypeNameValidator(string postCategoryTypeName, IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        var postCategoryType = postCategoryMetaLocator.GetTypeDetailByName(postCategoryTypeName);

        if (postCategoryType == null)
        {
            throw MarsValidationException.FromSingleError(nameof(PostDetail.Type), $"PostCategory type '{postCategoryTypeName}' not exist");
        }

        //RuleFor(x => x)
        //    .NotEmpty()
        //    .OverridePropertyName(nameof())
        //    .Must(v => metaModelTypesLocator.GetPostTypeByName(v) != null)
        //    .WithMessage(v => $"post type '{v}' not exist");
    }
}
