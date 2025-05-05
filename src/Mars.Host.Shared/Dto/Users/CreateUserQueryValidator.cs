using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Dto.Users.Phones;
using Mars.Host.Shared.Repositories;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Users;

public class CreateUserQueryValidator : AbstractValidator<CreateUserQuery>
{
    public CreateUserQueryValidator(IRoleRepository roleRepository)
    {
        RuleFor(x => x.Roles)
            .NotEmpty()
            .MustAsync(roleRepository.RolesExsists)
            .WithMessage(v => $"some roles not exist");

        RuleFor(x => x.Password).SetValidator(new UserPasswordValidator());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.Email != null);

        RuleFor(x => x.PhoneNumber)
            .SetValidator(new UserPhoneValidator())
            .When(x => x.PhoneNumber != null);

        //var roles = (await _roleRepository.ListAll(cancellationToken)).ToDictionary(s => s.Name);

        //if (!query.Roles.All(s => roles.ContainsKey(s)))
        //{
        //    throw new MarsValidationException(new Dictionary<string, string[]>()
        //    {
        //        [nameof(query.Roles)] = ["some roles not exist"],
        //    });
        //}
    }
}
