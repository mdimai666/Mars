using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public class DeletePostCategoryTypeQueryValidator : AbstractValidator<Guid>
{
    public DeletePostCategoryTypeQueryValidator(IPostCategoryTypeRepository userTypeRepository)
    {
        RuleFor(x => x)
            .CustomAsync(async (id, context, ct) =>
            {
                var userType = await userTypeRepository.Get(id, ct);

                if (userType == null)
                {
                    throw new NotFoundException($"PostCategoryType '{id}' not exist");
                }

                string[] undeletableTypes = ["default"];

                if (undeletableTypes.Contains(userType.TypeName))
                {
                    context.AddFailure(nameof(userType.TypeName), $"PostCategoryType '{userType.TypeName}' is internal type and cannot be delete");
                    return;
                }
            });
    }
}
