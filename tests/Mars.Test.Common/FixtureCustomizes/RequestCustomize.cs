using AutoFixture;
using Bogus;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.PostCategories;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.Users;
using static Mars.Test.Common.FixtureCustomizes.FixtureCustomize;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class RequestCustomize : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var faker = new Faker("en");

        fixture.Customize<CreatePostRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title, fixture.Create("Title - "))
                                    .With(s => s.Content, "<p>" + faker.Lorem.Paragraphs(4, "</p>\n<p>") + "</p>\n")
                                    .With(s => s.Status, PostStatusEntity.DefaultStatuses().TakeRandom().Slug)
                                    .With(s => s.Slug, TextTool.TranslateToPostSlug(fixture.Create("slug")))
                                    //.With(s => s.Image, "")
                                    //.With(s => s.Lang, Random.Shared.GetItems(["", "ru"], 1)[0])
                                    .With(s => s.Type, "post")
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.LangCode, Chance(["", "ru"]))
                                    .With(s => s.MetaValues, [])
                                    .With(s => s.CategoryIds, [])
                                    );

        fixture.Customize<UpdatePostRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title, fixture.Create("Title - "))
                                    .With(s => s.Content, "<p>" + faker.Lorem.Paragraphs(4, "</p>\n<p>") + "</p>\n")
                                    .With(s => s.Status, PostStatusEntity.DefaultStatuses().TakeRandom().Slug)
                                    .With(s => s.Slug, TextTool.TranslateToPostSlug(fixture.Create("slug")))
                                    //.With(s => s.Image, "")
                                    //.With(s => s.Lang, Random.Shared.GetItems(["", "ru"], 1)[0])
                                    .With(s => s.Type, "post")
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.LangCode, Chance(["", "ru"]))
                                    .With(s => s.MetaValues, [])
                                    .With(s => s.CategoryIds, [])
                                    );

        fixture.Customize<CreatePostTypeRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title)
                                    .With(s => s.TypeName, TextTool.TranslateToPostSlug(faker.Commerce.ProductName()).Left(Host.Data.Constants.PostTypeConstants.TypeNameMaxLength))
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 3)).ToList())
                                    .With(s => s.EnabledFeatures)
                                    .With(s => s.PostStatusList)
                                    .With(s => s.PostContentSettings)
                                    .With(s => s.MetaFields)
                                    );

        fixture.Customize<CreateFeedbackRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Title, faker.Lorem.Sentence())
                                    .With(s => s.Phone, Chance([faker.Phone.PhoneNumber(), null]))
                                    .With(s => s.Email, Chance([faker.Internet.Email(), null]))
                                    .With(s => s.FilledUsername, faker.Person.FullName)
                                    .With(s => s.Content, faker.Lorem.Paragraph(2))
                                    .With(s => s.Type, Chance(Enum.GetValues<FeedbackType>()).ToString())
                                    );

        fixture.Customize<CreateRoleRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Name, Chance(["tester", "moderator", "viwer", "zoo", "killer", "rabbit"]) + "-" + Guid.NewGuid().ToString().Left(8))
                                    );

        fixture.Customize<CreateUserRequest>(composer => composer
                                    .FromSeed(s => UserFixtureCustomizeExtension.User())
                                    .OmitAutoProperties()
                                );

        fixture.Customize<UpdateUserRequest>(composer => composer
                                    .FromSeed(s =>
                                    {
                                        var user = UserFixtureCustomizeExtension.User();
                                        return new UpdateUserRequest
                                        {
                                            Id = Guid.NewGuid(),
                                            UserName = user.UserName,
                                            Email = user.Email,
                                            FirstName = user.FirstName,
                                            LastName = user.LastName,
                                            MiddleName = user.MiddleName,
                                            Roles = user.Roles,
                                            BirthDate = user.BirthDate,
                                            Gender = user.Gender,
                                            PhoneNumber = user.PhoneNumber,
                                            AvatarUrl = user.AvatarUrl,
                                            MetaValues = [],
                                            Type = UserTypeEntity.DefaultTypeName,
                                        };
                                    })
                                    .OmitAutoProperties()
                                );

        var feedback = () => new Faker<CreateFeedbackRequest>("ru")
                                    .RuleFor(s => s.FilledUsername, f => f.Person.FullName)
                                    .RuleFor(s => s.Title, f => f.Name.JobTitle())
                                    .RuleFor(s => s.Content, f => f.Name.JobDescriptor())
                                    .RuleFor(s => s.Email, (f, s) => f.PickRandom(null, faker.Internet.Email(s.FilledUsername)))
                                    .RuleFor(s => s.Phone, f => f.PickRandom(PhoneUtil.NormalizePhone(f.Phone.PhoneNumber("+7 914 ### ## ##")), null, null))
                                    .RuleFor(s => s.Type, f => f.PickRandom(Enum.GetValues<FeedbackType>().Select(s => s.ToString())))
                                    .Generate();

        fixture.Customize<CreateFeedbackRequest>(composer => composer
                                    .FromSeed(s => feedback())
                                    .OmitAutoProperties()
                                );

        fixture.Customize<CreateNavMenuRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title)
                                    .With(s => s.Slug)
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 3)).ToList())
                                    .With(s => s.Class)
                                    .With(s => s.Style)
                                    .With(s => s.Roles, Array.Empty<string>())
                                    .With(s => s.MenuItems)
                                );

        fixture.Customize<UpdateNavMenuRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title)
                                    .With(s => s.Slug)
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 3)).ToList())
                                    .With(s => s.Class)
                                    .With(s => s.Style)
                                    .With(s => s.Roles, Array.Empty<string>())
                                    .With(s => s.MenuItems)
                                );

        fixture.Customize<CreatePostJsonRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title, fixture.Create("Title - "))
                                    .With(s => s.Content, "<p>" + faker.Lorem.Paragraphs(4, "</p>\n<p>") + "</p>\n")
                                    .With(s => s.Status, PostStatusEntity.DefaultStatuses().TakeRandom().Slug)
                                    .With(s => s.Slug, TextTool.TranslateToPostSlug(fixture.Create("slug")))
                                    //.With(s => s.Image, "")
                                    //.With(s => s.Lang, Random.Shared.GetItems(["", "ru"], 1)[0])
                                    .With(s => s.Type, "post")
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.LangCode, Chance(["", "ru"]))
                                    //.With(s => s.Meta, null)
                                    );

        fixture.Customize<UpdatePostJsonRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title, fixture.Create("Title - "))
                                    .With(s => s.Content, "<p>" + faker.Lorem.Paragraphs(4, "</p>\n<p>") + "</p>\n")
                                    .With(s => s.Status, PostStatusEntity.DefaultStatuses().TakeRandom().Slug)
                                    .With(s => s.Slug, TextTool.TranslateToPostSlug(fixture.Create("slug")))
                                    //.With(s => s.Image, "")
                                    //.With(s => s.Lang, Random.Shared.GetItems(["", "ru"], 1)[0])
                                    .With(s => s.Type, "post")
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.LangCode, Chance(["", "ru"]))
                                    //.With(s => s.Meta, null)
                                    );

        fixture.Customize<CreatePostCategoryTypeRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.Title, fixture.Create("PostCategoryType - "))
                                    .With(s => s.TypeName)
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.MetaFields)
                                    );

        fixture.Customize<CreatePostCategoryRequest>(composer => composer
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, (Guid?)null)
                                    .With(s => s.Title, fixture.Create("PostCategory - "))
                                    .With(s => s.Slug, TextTool.TranslateToPostSlug(fixture.Create("slug")))
                                    .With(s => s.Type, PostCategoryTypeEntity.DefaultTypeName)
                                    .With(s => s.PostType, "post")
                                    .With(s => s.Disabled, false)
                                    .With(s => s.Tags, Random.Shared.GetItems(TopTags, Random.Shared.Next(0, 6)).ToList())
                                    .With(s => s.MetaValues)
                                    );
    }
}
