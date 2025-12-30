using FluentValidation;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.UserTypes;

public class DeleteManyUserTypeQueryValidator : AbstractValidator<DeleteManyUserTypeQuery>
{
    public DeleteManyUserTypeQueryValidator(IUserTypeRepository userTypeRepository)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("UserType ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeleteUserTypeQueryValidator(userTypeRepository));
    }
}
