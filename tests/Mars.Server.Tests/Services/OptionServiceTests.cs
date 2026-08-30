using System.Text.Json;
using FluentAssertions;
using Mars.Options.Abstractions.Dto;
using Mars.Options.Abstractions.Exceptions;
using Mars.Options.Abstractions.Repositories;
using Mars.Options.Host.Services;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Contracts.Options;
using Mars.Test.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Server.Tests.Services;

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
        _optionService = new OptionService(mockServiceScopeFactory, em, mm, new TestHostEnvironment(), new ConfigurationManager());

        _optionService.RegisterOption<SiteSettings>();
    }

    [Fact]
    public void GetOption_GetSysOptions_NotBeNull()
    {
        // Arrange

        // Act
        var opt = _optionService.GetOption<SiteSettings>();

        // Assert
        opt.Should().NotBeNull();
    }

    [Fact]
    public void GetOption_GetSysOptionsMustSaveInLocalCache_LocalCacheExist()
    {
        // Arrange
        var tKey = typeof(SiteSettings);

        // Act
        var opt = _optionService.GetOption<SiteSettings>();

        // Assert
        opt.Should().NotBeNull();
        _optionService.localCache.Should().ContainKey(tKey);
        var cachedOpt = (_optionService.localCache[tKey] as SiteSettings);
        cachedOpt.Should().NotBeNull();
        cachedOpt.Should().Be(opt);
        _optionRepository.Received()
                            .GetKey<SiteSettings>(tKey.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetOption_GetSysOptionFromRepo_ReturnsFromRepoValue()
    {
        // Arrange
        var tKey = typeof(SiteSettings);
        var repoOpt = new SiteSettings()
        {
            SiteName = "test-site-" + Guid.NewGuid(),
        };
        _optionRepository.GetKey<SiteSettings>(tKey.Name)
            .Returns(repoOpt);

        // Act
        var opt = _optionService.GetOption<SiteSettings>();

        // Assert
        opt.Should().NotBeNull();
        _optionService.localCache.Should().ContainKey(tKey);
        _optionRepository.Received()
                            .GetKey<SiteSettings>(tKey.Name, Arg.Any<CancellationToken>());

        opt.SiteName.Should().Be(repoOpt.SiteName);
    }

    [Fact]
    public void GetOption_GetSysOptionsByClassName_NotBeNull()
    {
        // Arrange
        var tKey = typeof(SiteSettings);
        var key = tKey.Name;

        // Act
        var opt = _optionService.GetOptionByClass(key);

        // Assert
        opt.Should().NotBeNull();
    }

    [Fact]
    public void SetOption_SaveOptionMustSaveInLocalCache_SavesLocal()
    {
        // Arrange
        var opt = new SiteSettings()
        {
            SiteName = "test-site-" + Guid.NewGuid(),
        };

        // Act
        _ = nameof(OptionService.SaveOptionAsync);
        _optionService.SaveOption(opt);
        var savedOpt = _optionService.GetOption<SiteSettings>();

        // Assert
        savedOpt.Should().NotBeNull();
        savedOpt.SiteName.Should().Be(opt.SiteName);
        _optionRepository.Received()
                            .Create(Arg.Any<CreateOptionQuery<SiteSettings>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SetOption_SetSysOptionsByClassName_NotBeNull()
    {
        // Arrange
        var tKey = typeof(SiteSettings);
        var key = tKey.Name;
        var newName = "test-site-" + Guid.NewGuid();

        var opt = _optionService.GetOption<SiteSettings>();
        opt.SiteName = newName;
        var json = JsonSerializer.Serialize(opt);

        // Act
        _optionService.SetOptionByClass(key, json);

        // Assert
        var savedOpt = _optionService.localCache[tKey] as SiteSettings;
        savedOpt.Should().NotBeNull();
        savedOpt.SiteName.Should().Be(newName);
    }

    [Fact]
    public void SetOption_SetUnregisteredOptionByClassName_ThrowsOptionNotRegisteredException()
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
    public void RegisterOption_GetUnregisteredOption_ThrowsOptionNotRegisteredException()
    {
        // Arrange
        var tKey = typeof(ApiOption);

        // Act
        var action = () => _optionService.GetOption(tKey);

        // Assert
        action.Should().Throw<OptionNotRegisteredException>();
    }

    [Fact]
    public void RegisterOption_GetRegisteredOption_Succeeds()
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
