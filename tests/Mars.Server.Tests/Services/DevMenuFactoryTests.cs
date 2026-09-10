using FluentAssertions;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Host.Services;
using Mars.Contracts.Resources;

namespace Mars.Server.Tests.Services;

public class DevMenuFactoryTests
{
    [Fact]
    public void Build_ReturnsMenuWithFixedIdSlugAndSystemTag()
    {
        var menu = DevMenuFactory.Build([]);

        menu.Id.Should().Be(DevMenuFactory.DevMenuId);
        menu.Slug.Should().Be(DevMenuFactory.DevMenuSlug);
        menu.Title.Should().Be(DevMenuFactory.DevMenuTitle);
        menu.Tags.Should().Contain(DevMenuFactory.SystemTag);
        menu.IsPersisted.Should().BeFalse();
        menu.MenuItems.Should().NotBeEmpty();
        menu.MenuItems.Should().OnlyContain(s => s.IsSystem);
    }

    [Fact]
    public void Build_ReturnsStableItemIds_AcrossCalls()
    {
        var postTypes = new[] { PostType("news", "Новости"), PostType("products", "Товары") };

        var menu1 = DevMenuFactory.Build(postTypes);
        var menu2 = DevMenuFactory.Build(postTypes);

        menu1.MenuItems.Select(s => s.Id).Should().Equal(menu2.MenuItems.Select(s => s.Id));
    }

    [Fact]
    public void Build_PostTypeItems_StableIds_RegardlessOfInputOrder()
    {
        var menu1 = DevMenuFactory.Build([PostType("news", "Новости"), PostType("products", "Товары")]);
        var menu2 = DevMenuFactory.Build([PostType("products", "Товары"), PostType("news", "Новости")]);

        menu1.MenuItems.Select(s => s.Id).Should().BeEquivalentTo(menu2.MenuItems.Select(s => s.Id));
    }

    [Fact]
    public void Build_IncludesItemPerActivePostType_ExceptPost()
    {
        var menu = DevMenuFactory.Build([PostType("post", "Записи"), PostType("news", "Новости")]);

        menu.MenuItems.Should().Contain(s => s.Url == "/dev/Post/news");
        menu.MenuItems.Should().NotContain(s => s.Url == "/dev/Post/post" && s.Title != "Записи");
    }

    [Fact]
    public void Build_ExcludesComponentPostTypes()
    {
        var component = PostType("photo", "Фото") with { Visibility = PostTypeVisibility.Component };
        var menu = DevMenuFactory.Build([PostType("news", "Новости"), component]);

        menu.MenuItems.Should().Contain(s => s.Url == "/dev/Post/news");
        menu.MenuItems.Should().NotContain(s => s.Url == "/dev/Post/photo");
    }

    [Fact]
    public void Merge_DbCopyOverridesDefaults_AndKeepsDisabledState()
    {
        var defaults = DevMenuFactory.Build([]);
        var itemToDisable = defaults.MenuItems.First(s => s.Title == "Настройки");
        var dbItems = defaults.MenuItems.Select(s => s.Id == itemToDisable.Id ? s with { Disabled = true } : s).ToList();
        var dbMenu = defaults with { Title = "Мой dev", MenuItems = dbItems };

        var merged = DevMenuFactory.Merge(dbMenu, defaults);

        merged.Title.Should().Be("Мой dev");
        merged.MenuItems.Count.Should().Be(defaults.MenuItems.Count);
        merged.MenuItems.First(s => s.Id == itemToDisable.Id).Disabled.Should().BeTrue();
        merged.IsPersisted.Should().BeTrue();
    }

    [Fact]
    public void Merge_AddsMissingDefaultItem_InDefaultPosition()
    {
        var defaults = DevMenuFactory.Build([]);
        var settings = defaults.MenuItems.First(s => s.Title == "Настройки");
        var plugins = defaults.MenuItems.First(s => s.Title == AppRes.Plugins);
        var dbMenu = defaults with { MenuItems = defaults.MenuItems.Where(s => s.Id != settings.Id).ToList() };

        var merged = DevMenuFactory.Merge(dbMenu, defaults);

        var mergedList = merged.MenuItems.ToList();
        mergedList.Should().Contain(s => s.Id == settings.Id);
        mergedList.FindIndex(s => s.Id == settings.Id).Should().Be(mergedList.FindIndex(s => s.Id == plugins.Id) + 1);
    }

    [Fact]
    public void Merge_NewPostType_AppearsInMergedMenu()
    {
        // сохранили меню без типа "news", потом тип создали
        var dbMenu = DevMenuFactory.Build([]) with { IsPersisted = true };
        var defaultsWithNews = DevMenuFactory.Build([PostType("news", "Новости")]);

        var merged = DevMenuFactory.Merge(dbMenu, defaultsWithNews);

        merged.MenuItems.Should().Contain(s => s.Url == "/dev/Post/news" && s.IsSystem);
    }

    [Fact]
    public void Merge_CustomItems_KeptAndFlaggedNotSystem()
    {
        var defaults = DevMenuFactory.Build([]);
        var custom = new NavMenuItemDto
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.Empty,
            Title = "Кастомный пункт",
            Url = "/dev/custom",
            Icon = "",
            Roles = [],
            RolesInverse = false,
            Class = "",
            Style = "",
            OpenInNewTab = false,
            Disabled = false,
            IsHeader = false,
            IsDivider = false,
        };
        var dbMenu = defaults with { MenuItems = [.. defaults.MenuItems, custom] };

        var merged = DevMenuFactory.Merge(dbMenu, defaults);

        var mergedCustom = merged.MenuItems.First(s => s.Id == custom.Id);
        mergedCustom.Title.Should().Be("Кастомный пункт");
        mergedCustom.IsSystem.Should().BeFalse();
        merged.MenuItems.Where(s => s.IsSystem).Count().Should().Be(defaults.MenuItems.Count);
    }

    [Fact]
    public void Merge_ForcesSystemTagSlugAndPersistedFlag()
    {
        var defaults = DevMenuFactory.Build([]);
        var dbMenu = defaults with { Slug = "hacked", Tags = ["custom-tag"] };

        var merged = DevMenuFactory.Merge(dbMenu, defaults);

        merged.Slug.Should().Be(DevMenuFactory.DevMenuSlug);
        merged.Tags.Should().Contain(DevMenuFactory.SystemTag).And.Contain("custom-tag");
        merged.IsPersisted.Should().BeTrue();
    }

    static PostTypeSummary PostType(string typeName, string title)
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Title = title,
            TypeName = typeName,
            Tags = [],
            EnabledFeatures = [],
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
        };
}
