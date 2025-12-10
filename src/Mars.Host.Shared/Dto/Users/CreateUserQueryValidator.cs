using FluentValidation;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Dto.Users.Phones;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Users;

public class CreateUserQueryValidator : AbstractValidator<CreateUserQuery>
{
    public CreateUserQueryValidator(IRoleRepository roleRepository, IUserTypeRepository userTypeRepository, IUserRepository userRepository)
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

        RuleFor(x => x.UserName)
            .SetValidator(new UserNameRuleValidator())
            .SetValidator(new UsernameBlacklistValidator());

        RuleFor(x => x.Type)
            .MustAsync(userTypeRepository.TypeNameExist)
            .WithMessage(v => $"user type '{v.Type}' does not exist");

        RuleFor(x => x.UserName)
            .MustAsync(async (ctx, x, ct) => !await userRepository.UserNameExistAsync(x, ct))
            .WithMessage("This username is already taken by another user.");

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
