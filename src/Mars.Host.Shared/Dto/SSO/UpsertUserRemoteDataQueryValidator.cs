using FluentValidation;
using Mars.Host.Shared.Dto.Users.Phones;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.SSO;

public class UpsertUserRemoteDataQueryValidator : AbstractValidator<UpsertUserRemoteDataQuery>
{
    public UpsertUserRemoteDataQueryValidator(IRoleRepository roleRepository, IUserTypeRepository userTypeRepository, IUserRepository userRepository)
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .SetValidator(new UserPhoneValidator())
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        //RuleFor(x => x.PreferredUserName)
        //    .SetValidator(new UserNameRuleValidator())
        //    .SetValidator(new UsernameBlacklistValidator());
    }
}
