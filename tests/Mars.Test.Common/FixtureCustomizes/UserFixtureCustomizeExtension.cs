using AutoFixture;
using Bogus;
using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Users;
using Microsoft.EntityFrameworkCore;

namespace Mars.Test.Common.FixtureCustomizes;

public static class UserFixtureCustomizeExtension
{
    public static CreateUserRequest User()
        => User(new Faker("ru"));

    public static CreateUserRequest User(Faker faker, IReadOnlyCollection<CreateMetaValueRequest>? metaValues = null)
        => new Faker<CreateUserRequest>("ru")
            .RuleFor(s => s.UserName, f => f.Person.UserName)
            .RuleFor(s => s.FirstName, f => f.Person.FirstName)
            .RuleFor(s => s.LastName, f => f.Person.LastName)
            .RuleFor(s => s.Email, (f, s) => faker.Internet.Email(s.FirstName, s.LastName))
            .RuleFor(s => s.PhoneNumber, f => f.PickRandom(PhoneUtil.NormalizePhone(f.Phone.PhoneNumber("+7 (###) ### ## ##")), null, null))
            .RuleFor(s => s.Password, Password.Generate(6, 2))
            .RuleFor(s => s.Roles, ["Viewer"])
            .RuleFor(s => s.BirthDate, new DateTime(1991, 6, 10))
            .RuleFor(s => s.Gender, UserGender.Male)
            .RuleFor(s => s.Type, UserTypeEntity.DefaultTypeName)
            .RuleFor(s => s.MetaValues, metaValues ?? [])
            .Generate();

    public static CreateUserRequest CreateUserWithMetaValues(this IFixture fixture, Guid metaFieldId, EMetaFieldType type)
    {
        var faker = new Faker("ru");

        var user = User(faker, [fixture.CreateSimpleCreateMetaValueRequest(metaFieldId, type)]);

        return user;
    }

    public static async Task<UserEntity> AppendUserTypeMetaFieldsAndCreateUserWithMetaValues(this IFixture _fixture, MarsDbContext ef)
    {
        var createdUser = _fixture.Create<UserEntity>();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray();
        var userType = ef.UserTypes.Include(s => s.MetaFields).First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        userType.MetaFields = [.. metaFields];
        var metaValues = metaFields.Select(mf =>
        {
            var mv = _fixture.MetaValueEntity(mf.Id, mf.Type);
            mv.MetaField = mf;
            return mv;
        }).ToList();
        createdUser.MetaValues = metaValues;

        await ef.Users.AddAsync(createdUser);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        return createdUser;
    }
}
