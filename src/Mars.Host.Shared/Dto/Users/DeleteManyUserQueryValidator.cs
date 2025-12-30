using FluentValidation;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Users;

public class DeleteManyUserQueryValidator : AbstractValidator<DeleteManyUserQuery>
{
    public DeleteManyUserQueryValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("User ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeleteUserQueryValidator(userRepository));

        //TODO: переделать. чтобы оно не удаляло единственного админа.
    }
}
