using Mars.Core.Exceptions;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.Posts;

public class PostTypeNameValidator
{
    public PostTypeNameValidator(string postTypeName, IMetaModelTypesLocator metaModelTypesLocator)
    {
        var postType = metaModelTypesLocator.GetPostTypeByName(postTypeName);

        if (postType == null)
        {
            throw new MarsValidationException(new Dictionary<string, string[]>
            {
                [nameof(PostDetail.Type)] = [$"post type '{postTypeName}' not exist"]
            });
        }

        //RuleFor(x => x)
        //    .NotEmpty()
        //    .OverridePropertyName(nameof())
        //    .Must(v => metaModelTypesLocator.GetPostTypeByName(v) != null)
        //    .WithMessage(v => $"post type '{v}' not exist");
    }
}
