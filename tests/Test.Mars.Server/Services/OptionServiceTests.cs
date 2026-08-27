using System.Text.Json;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Options;
using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Repositories;
using Mars.Options.Models;
using Mars.Shared.Options;
using Mars.Test.Common;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Test.Mars.Host.Services;

public class OptionServiceTests
{
    private readonly OptionService _optionService;
    private readonly IOptionRepository _optionRepository;

    public OptionServiceTests()
    {
        _optionRepository = Substitute.For<IOptionRepository>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IOptionRepository>(x => _optionRepository);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var mockServiceScopeFactory = Substitute.For<IServiceScopeFactory>();
        mockServiceScopeFactory.CreateScope().ReturnsForAnyArgs(_ =>
        {
            var mockServiceScope = Substitute.For<IServiceScope>();
            mockServiceScope.ServiceProvider.Returns(serviceProvider);
            return mockServiceScope;
        });

        var em = Substitute.For<IEventManager>();
        var mm = Substitute.For<IMemoryCache>();
        _optionService = new OptionService(mockServiceScopeFactory, em, mm, new TestHostEnvironment());

        _optionService.RegisterOption<SysOptions>();
    }

    [Fact]
    public void GetOption_GetSysOptions_NotBeNull()
    {
        // Arrange

        // Act
        var opt = _optionService.GetOption<SysOptions>();

        // Assert
        opt.Should().NotBeNull();
    }

    [Fact]
    public void GetOption_GetSysOptionsMustSaveInLocalCache_LocalCacheExist()
    {
        // Arrange
        var tKey = typeof(SysOptions);

        // Act
        var opt = _optionService.GetOption<SysOptions>();

        // Assert
        opt.Should().NotBeNull();
        _optionService.localCache.Should().ContainKey(tKey);
        var cachedOpt = (_optionService.localCache[tKey] as SysOptions);
        cachedOpt.Should().NotBeNull();
        cachedOpt.Should().Be(opt);
        _optionRepository.Received()
                            .GetKey<SysOptions>(tKey.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetOption_GetSysOptionFromRepo_ShouldReturnFromRepoValue()
    {
        // Arrange
        var tKey = typeof(SysOptions);
        var repoOpt = new SysOptions()
        {
            SiteName = "test-site-" + Guid.NewGuid(),
        };
        _optionRepository.GetKey<SysOptions>(tKey.Name)
            .Returns(repoOpt);

        // Act
        var opt = _optionService.GetOption<SysOptions>();

        // Assert
        opt.Should().NotBeNull();
        _optionService.localCache.Should().ContainKey(tKey);
        _optionRepository.Received()
                            .GetKey<SysOptions>(tKey.Name, Arg.Any<CancellationToken>());

        opt.SiteName.Should().Be(repoOpt.SiteName);
    }

    [Fact]
    public void GetOption_GetSysOptionsByClassName_NotBeNull()
    {
        // Arrange
        var tKey = typeof(SysOptions);
        var key = tKey.Name;

        // Act
        var opt = _optionService.GetOptionByClass(key);

        // Assert
        opt.Should().NotBeNull();
    }

    [Fact]
    public void SetOption_SaveOptionMustSaveInLocalCache_ShouldSaveLocal()
    {
        // Arrange
        var opt = new SysOptions()
        {
            SiteName = "test-site-" + Guid.NewGuid(),
        };

        // Act
        _ = nameof(OptionService.SaveOptionAsync);
        _optionService.SaveOption(opt);
        var savedOpt = _optionService.GetOption<SysOptions>();

        // Assert
        savedOpt.Should().NotBeNull();
        savedOpt.SiteName.Should().Be(opt.SiteName);
        _optionRepository.Received()
                            .Create(Arg.Any<CreateOptionQuery<SysOptions>>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public void SetOption_SetSysOptionsByClassName_NotBeNull()
    {
        // Arrange
        var tKey = typeof(SysOptions);
        var key = tKey.Name;
        var newName = "test-site-" + Guid.NewGuid();

        var opt = _optionService.GetOption<SysOptions>();
        opt.SiteName = newName;
        var json = JsonSerializer.Serialize(opt);

        // Act
        _optionService.SetOptionByClass(key, json);

        // Assert
        var savedOpt = _optionService.localCache[tKey] as SysOptions;
        savedOpt.Should().NotBeNull();
        savedOpt.SiteName.Should().Be(newName);
    }

    [Fact]
    public void SetOption_SetUnregisteredOptionByClassName_ShouldOptionNotRegisteredException()
    {
        // Arrange
        var tKey = typeof(ApiOption);
        var key = tKey.Name;

        // Act
        var action = () => _optionService.SetOptionByClass(key, "{}");

        // Assert
        action.Should().Throw<OptionNotRegisteredException>();
    }

    [Fact]
    public void RegisterOption_GetUnregisteredOption_ShouldOptionNotRegisteredException()
    {
        // Arrange
        var tKey = typeof(ApiOption);

        // Act
        var action = () => _optionService.GetOption(tKey);

        // Assert
        action.Should().Throw<OptionNotRegisteredException>();
    }

    [Fact]
    public void RegisterOption_GetRegisteredOption_ShouldSuccess()
    {
        // Arrange
        var tKey = typeof(ApiOption);
        _optionService.RegisterOption<ApiOption>();

        // Act
        var action = () => _optionService.GetOption(tKey);

        // Assert
        action.Should().NotThrow<OptionNotRegisteredException>();
    }
}
