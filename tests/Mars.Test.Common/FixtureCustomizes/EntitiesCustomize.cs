using AutoFixture;
using Bogus;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Mars.Host.Data.OwnedTypes.Users;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.Constants;
using static Mars.Test.Common.FixtureCustomizes.FixtureCustomize;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class EntitiesCustomize : ICustomization
{
    public static Dictionary<string, PostTypeEntity> PostTypeDict = default!;
    public static Dictionary<string, UserTypeEntity> UserTypeDict = new() { [UserTypeEntity.DefaultTypeName] = UserConstants.TestUserType };

    public void Customize(IFixture fixture)
    {
        var faker = new Faker("en");

        var user = () => new Faker<UserEntity>("ru")
            .RuleFor(s => s.Id, Guid.NewGuid())
            .RuleFor(s => s.FirstName, f => f.Person.FirstName)
            .RuleFor(s => s.LastName, f => f.Person.LastName)
            .RuleFor(s => s.UserName, f => f.Person.UserName)
            .RuleFor(s => s.NormalizedUserName, (f, s) => s.UserName.ToUpper())
            .RuleFor(s => s.Email, (f, s) => faker.Internet.Email(s.FirstName, s.LastName))
            .RuleFor(s => s.NormalizedEmail, (f, s) => s.Email.ToUpper())
            .RuleFor(s => s.PhoneNumber, f => f.PickRandom(PhoneUtil.NormalizePhone(f.Phone.PhoneNumber("+7 (###) ### ## ##")), null, null))
            .RuleFor(s => s.Gender, f => f.PickRandom<UserGender>())
            .RuleFor(s => s.SecurityStamp, Guid.NewGuid().ToString())
            .RuleFor(s => s.Status, EUserStatus.Activated)
            .RuleFor(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
            .RuleFor(s => s.UserTypeId, UserTypeDict[UserTypeEntity.DefaultTypeName].Id)
            .RuleFor(s => s.MetaValues, [])
            .Generate();

        fixture.Customize<UserEntity>(composer => composer
                                    .FromSeed(s => user())
                                    .OmitAutoProperties()
                                );

        fixture.Customize<UserTypeEntity>(composer => composer
                                   .OmitAutoProperties()
                                   .With(s => s.Id)
                                   .With(s => s.Title)
                                   .With(s => s.TypeName)
                                   .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                   );

        fixture.Customize<RoleEntity>(composer => composer
                                   .FromSeed(role =>
                                   {
                                       role ??= new RoleEntity();
                                       var roleName = "role-" + Guid.NewGuid().ToString("N");
                                       role.Name = roleName;
                                       role.NormalizedName = roleName.ToUpper();
                                       return role;
                                   })
                                   .OmitAutoProperties()
                                   .With(s => s.Id)
                                   .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                   );

        fixture.Customize<PostContentSettings>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.PostContentType, PostTypeConstants.DefaultPostContentTypes.PlainText)
                                    );

        fixture.Customize<PostTypeEntity>(composer => composer
                                   .OmitAutoProperties()
                                   .With(s => s.Id)
                                   .With(s => s.Title, () => fixture.Create("Title - "))
                                   //.With(s => s.PostStatusList, PostStatus.DefaultStatuses())
                                   .With(s => s.TypeName)
                                   .With(s => s.PostContentType)
                                   .With(s => s.EnabledFeatures, [nameof(PostEntity.Content)])
                                   .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                   //.With(s => s.ModifiedAt, DateTime.MinValue)
                                   //.Without(s => s.ViewSettings)
                                   );

        fixture.Customize<PostEntity>(composer => composer
                                    .FromFactory(() =>
                                    {
                                        var id = Guid.NewGuid();
                                        return new()
                                        {
                                            Id = id,
                                            Slug = TextTool.TranslateToPostSlug("slug-" + id)
                                        };
                                    })
                                   .OmitAutoProperties()
                                   .With(s => s.Id)
                                   .With(s => s.Title, () => fixture.Create("Title - "))
                                   .With(s => s.Content, () => "<p>" + faker.Lorem.Paragraphs(4, "</p>\n<p>") + "</p>\n")
                                   .With(s => s.Status, () => PostStatusEntity.DefaultStatuses().TakeRandom().Slug)
                                   //.With(s => s.Image, "")
                                   .With(s => s.LangCode, () => Random.Shared.GetItems(["", "ru"], 1)[0])
                                   //.With(s => s.Type, "post")
                                   .With(s => s.PostTypeId, PostTypeDict["post"].Id)
                                   .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                   //.With(s => s.ModifiedAt, null!)
                                   .With(s => s.Tags, () => Random.Shared.GetItems(FixtureCustomize.TopTags, Random.Shared.Next(0, 6)).ToList())
                                   .With(s => s.UserId, UserConstants.TestUserId)
                                   //.Without(s => s.FileList)
                                   //.Without(s => s.PostFiles)
                                   //.Without(s => s.MetaValues)
                                   //.Without(s => s.PostMetaValues)
                                   //.Without(s => s.Comments)
                                   //.Without(s => s.CommentsCount)
                                   //.Without(s => s.Likes)
                                   //.Without(s => s.LikesCount)
                                   //.Without(s => s.ParentId)
                                   //.Without(s => s.CategoryId)
                                   //.Without(s => s.User)
                                   );

        var fixtureFileExt = "txt";

        fixture.Customize<FileEntity>(composer => composer
                                   .OmitAutoProperties()
                                   .With(s => s.Id)
                                   .With(s => s.FileName, () => faker.System.FileName(fixtureFileExt))
                                   .With(s => s.FileExt, fixtureFileExt)
                                   .With(s => s.FileSize, () => (ulong)Random.Shared.Next(100, 2_000_000))
                                   .With(s => s.FilePhysicalPath, () => $"Media/file-{Guid.NewGuid()}.{fixtureFileExt}")
                                   .With(s => s.FileVirtualPath, () => $"Media/file-{Guid.NewGuid()}.{fixtureFileExt}")
                                   .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                   .With(s => s.UserId, UserConstants.TestUserId)
                                   .Without(s => s.Meta)
                                   );

        fixture.Customize<NavMenuEntity>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title)
                                    .With(s => s.Slug)
                                    .With(s => s.CreatedAt, FixtureCustomize.DefaultCreated)
                                    .With(s => s.Tags, () => Random.Shared.GetItems(FixtureCustomize.TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.MenuItems)
                                   );

        fixture.Customize<NavMenuItem>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title)
                                    .With(s => s.Url)
                                   );

        fixture.Customize<MetaFieldEntity>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.Title)
                                    .With(s => s.Key)
                                    .With(s => s.Type, Chance([EMetaFieldType.String, EMetaFieldType.Int, EMetaFieldType.Bool, EMetaFieldType.DateTime]))
                                    .With(s => s.Order)
                                    .With(s => s.Tags)
                                    .With(s => s.Variants)
                                   );

        fixture.Customize<MetaValueEntity>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.Type, EMetaFieldType.String)
                                    .With(s => s.VariantsIds, [])
                                   );
    }

}
