using FluentValidation;
using Mars.Identity.Abstractions.Repositories;

namespace Mars.Identity.Abstractions.Dto.UserTypes;

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
