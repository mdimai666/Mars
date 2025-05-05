using Mars.Host.Shared.Dto.Users.Phones;
using Mars.Host.Shared.Repositories;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Users;

public class UpdateUserQueryValidator : AbstractValidator<UpdateUserQuery>
{
    public UpdateUserQueryValidator(IRoleRepository roleRepository)
    {
        RuleFor(x => x.Roles)
            .NotEmpty()
            .MustAsync(roleRepository.RolesExsists)
            .WithMessage(v => $"some roles not exist");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.Email != null);

        RuleFor(x => x.PhoneNumber)
            .SetValidator(new UserPhoneValidator())
            .When(x => x.PhoneNumber != null);
    }
}
