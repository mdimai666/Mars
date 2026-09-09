using Mars.Data.Entities;
using Mars.Test.Common.Constants;

namespace Mars.Test.Common.FixtureCustomizes;

/// <summary>
/// Ссылки на базовые сид-сущности тестовой БД (посттипы, типы юзеров, типы категорий).
/// Экземпляр привязан к конкретной засиженной БД/фикстуре — в отличие от прежних статических
/// словарей, параллельные фикстуры не могут перетереть данные друг друга.
/// </summary>
public sealed class TestEntityRefs
{
    public const string DefaultPostTypeName = "post";

    public IReadOnlyDictionary<string, PostTypeEntity> PostTypes { get; }
    public IReadOnlyDictionary<string, UserTypeEntity> UserTypes { get; }
    public IReadOnlyDictionary<string, PostCategoryTypeEntity> PostCategoryTypes { get; }

    public TestEntityRefs(
        IReadOnlyDictionary<string, PostTypeEntity> postTypes,
        IReadOnlyDictionary<string, UserTypeEntity> userTypes,
        IReadOnlyDictionary<string, PostCategoryTypeEntity> postCategoryTypes)
    {
        PostTypes = postTypes;
        UserTypes = userTypes;
        PostCategoryTypes = postCategoryTypes;
    }

    /// <summary>Посттип «post» — сущность из засиженной БД текущей фикстуры.</summary>
    public PostTypeEntity PostType => PostTypes[DefaultPostTypeName];

    /// <summary>Тип юзера по умолчанию — сущность из засиженной БД текущей фикстуры.</summary>
    public UserTypeEntity UserType => UserTypes[UserTypeEntity.DefaultTypeName];

    /// <summary>Тип категории по умолчанию — сущность из засиженной БД текущей фикстуры.</summary>
    public PostCategoryTypeEntity PostCategoryType => PostCategoryTypes[PostCategoryTypeEntity.DefaultTypeName];

    /// <summary>
    /// Заглушка для юнит-тестов без БД: минимальные сущности с пустыми Id.
    /// Эквивалентна тому, что тесты раньше вручную подставляли в статические словари.
    /// </summary>
    public static TestEntityRefs CreateDefault() => new(
        new Dictionary<string, PostTypeEntity>
        {
            [DefaultPostTypeName] = new() { TypeName = DefaultPostTypeName },
        },
        new Dictionary<string, UserTypeEntity>
        {
            [UserTypeEntity.DefaultTypeName] = UserConstants.TestUserType,
        },
        new Dictionary<string, PostCategoryTypeEntity>
        {
            [PostCategoryTypeEntity.DefaultTypeName] = new() { TypeName = PostCategoryTypeEntity.DefaultTypeName },
        });
}
