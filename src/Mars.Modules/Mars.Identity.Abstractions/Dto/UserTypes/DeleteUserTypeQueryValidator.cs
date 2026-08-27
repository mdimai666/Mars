using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.UserTypes;

public class DeleteUserTypeQueryValidator : AbstractValidator<Guid>
{
    public DeleteUserTypeQueryValidator(IUserTypeRepository userTypeRepository)
    {
        RuleFor(x => x)
            .CustomAsync(async (id, context, ct) =>
            {
                var userType = await userTypeRepository.Get(id, ct);

                if (userType == null)
                {
                    throw new NotFoundException($"user type '{id}' not exist");
                }

                string[] undeletableTypes = ["default"];

                if (undeletableTypes.Contains(userType.TypeName))
                {
                    context.AddFailure(nameof(userType.TypeName), $"user type '{userType.TypeName}' is internal type and cannot be delete");
                    return;
                }
            });
    }
}
