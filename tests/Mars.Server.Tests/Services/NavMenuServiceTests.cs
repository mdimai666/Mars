using FluentAssertions;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Host.Services;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Managers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Test.Mars.Server.Services;

public class NavMenuServiceTests
{
    readonly NavMenuService _service;
    readonly INavMenuRepository _navMenuRepository;
    readonly IPostTypeRepository _postTypeRepository;

    public NavMenuServiceTests()
    {
        _navMenuRepository = Substitute.For<INavMenuRepository>();
        _postTypeRepository = Substitute.For<IPostTypeRepository>();
        _postTypeRepository.ListAllActive(Arg.Any<CancellationToken>()).Returns([]);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<INavMenuRepository>(_ => _navMenuRepository);
        serviceCollection.AddScoped<IPostTypeRepository>(_ => _postTypeRepository);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().ReturnsForAnyArgs(_ =>
        {
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            return scope;
        });

        var eventManager = Substitute.For<IEventManager>();
        eventManager.Defaults.Returns(new EventManagerDefaults());

        _service = new NavMenuService(scopeFactory, new MemoryCache(new MemoryCacheOptions()), eventManager);
    }

    [Fact]
    public void DevMenu_WithoutDbCopy_ReturnsDefaultGeneratedMenu()
    {
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuDetail?)null);

        var menu = _service.DevMenu();

        menu.Id.Should().Be(DevMenuFactory.DevMenuId);
        menu.IsPersisted.Should().BeFalse();
        menu.Tags.Should().Contain(DevMenuFactory.SystemTag);
        menu.MenuItems.Should().NotBeEmpty();
    }

    [Fact]
    public void DevMenu_WithDbCopy_ReturnsMergedMenu()
    {
        var dbCopy = DevMenuFactory.Build([]) with { Title = "Мой dev", IsPersisted = true };
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns(dbCopy);

        var menu = _service.DevMenu();

        menu.Title.Should().Be("Мой dev");
        menu.IsPersisted.Should().BeTrue();
    }

    [Fact]
    public async Task GetDetail_DevMenuId_ReturnsMenu_EvenWhenNotPersisted()
    {
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuDetail?)null);

        var detail = await _service.GetDetail(DevMenuFactory.DevMenuId, CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.Id.Should().Be(DevMenuFactory.DevMenuId);
    }

    [Fact]
    public async Task Update_DevMenuFirstSave_CreatesDbCopyWithFixedIdAndSystemTag()
    {
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuSummary?)null);
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuDetail?)null);

        await _service.Update(UpdateQuery(DevMenuFactory.DevMenuId), CancellationToken.None);

        await _navMenuRepository.Received(1).Create(
            Arg.Is<CreateNavMenuQuery>(q => q.Id == DevMenuFactory.DevMenuId
                                            && q.Slug == DevMenuFactory.DevMenuSlug
                                            && q.Tags.Contains(DevMenuFactory.SystemTag)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_PersistedDevMenu_UpdatesNotCreates()
    {
        var summary = MenuSummary(DevMenuFactory.DevMenuId, DevMenuFactory.DevMenuSlug);
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>()).Returns(summary);

        await _service.Update(UpdateQuery(DevMenuFactory.DevMenuId), CancellationToken.None);

        await _navMenuRepository.Received(1).Update(Arg.Any<UpdateNavMenuQuery>(), Arg.Any<CancellationToken>());
        await _navMenuRepository.DidNotReceiveWithAnyArgs().Create(default!, default);
    }

    [Fact]
    public async Task Update_RegularMenuNotExists_ThrowsNotFound()
    {
        var otherId = Guid.NewGuid();
        _navMenuRepository.Get(otherId, Arg.Any<CancellationToken>()).Returns((NavMenuSummary?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(UpdateQuery(otherId), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_DevMenu_ThrowsUserAction()
    {
        await Assert.ThrowsAsync<UserActionException>(() => _service.Delete(DevMenuFactory.DevMenuId, CancellationToken.None));
        await _navMenuRepository.DidNotReceiveWithAnyArgs().Delete(default, default);
    }

    [Fact]
    public async Task DeleteMany_WithDevMenu_ThrowsUserAction()
    {
        var query = new DeleteManyNavMenuQuery { Ids = [Guid.NewGuid(), DevMenuFactory.DevMenuId] };

        await Assert.ThrowsAsync<UserActionException>(() => _service.DeleteMany(query, CancellationToken.None));
    }

    [Fact]
    public async Task Reset_DevMenu_DeletesDbCopy()
    {
        var summary = MenuSummary(DevMenuFactory.DevMenuId, DevMenuFactory.DevMenuSlug);
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>()).Returns(summary);

        await _service.Reset(DevMenuFactory.DevMenuId, CancellationToken.None);

        await _navMenuRepository.Received(1).Delete(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reset_DevMenuNotPersisted_NoDelete()
    {
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuSummary?)null);

        await _service.Reset(DevMenuFactory.DevMenuId, CancellationToken.None);

        await _navMenuRepository.DidNotReceiveWithAnyArgs().Delete(default, default);
    }

    [Fact]
    public async Task Reset_RegularMenu_ThrowsUserAction()
    {
        await Assert.ThrowsAsync<UserActionException>(() => _service.Reset(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListForAdmin_WhenDevMenuNotPersisted_InjectsVirtualEntry()
    {
        var repoResult = new ListDataResult<NavMenuSummary>([MenuSummary(Guid.NewGuid(), "footer")], false, 1);
        _navMenuRepository.List(Arg.Any<ListNavMenuQuery>(), Arg.Any<CancellationToken>()).Returns(repoResult);
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuSummary?)null);

        var result = await _service.ListForAdmin(new ListNavMenuQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.First().Id.Should().Be(DevMenuFactory.DevMenuId);
        result.Items.First().Tags.Should().Contain(DevMenuFactory.SystemTag);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListForAdmin_WhenDevMenuPersisted_NoInjection()
    {
        var repoResult = new ListDataResult<NavMenuSummary>([MenuSummary(Guid.NewGuid(), "footer")], false, 1);
        _navMenuRepository.List(Arg.Any<ListNavMenuQuery>(), Arg.Any<CancellationToken>()).Returns(repoResult);
        _navMenuRepository.Get(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns(MenuSummary(DevMenuFactory.DevMenuId, DevMenuFactory.DevMenuSlug));

        var result = await _service.ListForAdmin(new ListNavMenuQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public void GetAppInitialDataMenus_Public_ExcludesSystemMenus()
    {
        var regular = MenuDetail(Guid.NewGuid(), "footer");
        var system = MenuDetail(Guid.NewGuid(), "internal", DevMenuFactory.SystemTag);
        _navMenuRepository.ListAllActiveDetail(Arg.Any<ListAllNavMenuQuery>(), Arg.Any<CancellationToken>())
                          .Returns([regular, system]);
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>())
                          .Returns((NavMenuDetail?)null);

        var publicMenus = _service.GetAppInitialDataMenus(includeDevMenu: false);
        var adminMenus = _service.GetAppInitialDataMenus(includeDevMenu: true);

        publicMenus.Should().ContainSingle().Which.Slug.Should().Be("footer");
        adminMenus.Select(s => s.Slug).Should().BeEquivalentTo(["footer", DevMenuFactory.DevMenuSlug]);
    }

    [Fact]
    public void GetAppInitialDataMenus_Admin_DevMenuRespectsDisabledItems()
    {
        _navMenuRepository.ListAllActiveDetail(Arg.Any<ListAllNavMenuQuery>(), Arg.Any<CancellationToken>())
                          .Returns([]);

        var defaults = DevMenuFactory.Build([]);
        var itemToDisable = defaults.MenuItems.First(s => s.Title == "Настройки");
        var dbCopy = defaults with
        {
            IsPersisted = true,
            MenuItems = defaults.MenuItems.Select(s => s.Id == itemToDisable.Id ? s with { Disabled = true } : s).ToList(),
        };
        _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, Arg.Any<CancellationToken>()).Returns(dbCopy);

        var adminMenus = _service.GetAppInitialDataMenus(includeDevMenu: true);

        var devMenu = adminMenus.Single(s => s.Slug == DevMenuFactory.DevMenuSlug);
        devMenu.MenuItems.Should().NotContain(s => s.Id == itemToDisable.Id);
    }

    static NavMenuSummary MenuSummary(Guid id, string slug, params string[] tags)
        => new()
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Title = slug,
            Slug = slug,
            Disabled = false,
            Tags = tags,
        };

    static NavMenuDetail MenuDetail(Guid id, string slug, params string[] tags)
        => new()
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Title = slug,
            Slug = slug,
            Disabled = false,
            Tags = tags,
            ModifiedAt = null,
            MenuItems = [],
            Class = "",
            Style = "",
            Roles = [],
            RolesInverse = false,
        };

    static UpdateNavMenuQuery UpdateQuery(Guid id)
        => new()
        {
            Id = id,
            Title = "Dev",
            Slug = "hacked",
            Disabled = false,
            Tags = [],
            MenuItems = [],
            Class = "",
            Style = "",
            Roles = [],
            RolesInverse = false,
        };
}
