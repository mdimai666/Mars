using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategories;

public class DeletePostCategoryQueryValidator : AbstractValidator<Guid>
{
    public DeletePostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        //RuleFor(x => x)
        //    .CustomAsync(async (id, context, ct) =>
        //    {
        //        var post = await postCategoryRepository.Get(id, ct);

        //        if (post == null)
        //        {
        //            throw new NotFoundException($"PostCategory '{id}' not exist");
        //        }
        //    });

        //TODO: Что делать с детьми?
    }
}
