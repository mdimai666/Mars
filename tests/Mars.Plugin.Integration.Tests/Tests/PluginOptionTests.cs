using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PluginExample;
using PluginExample.Options;

namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginOptionTests : BasePluginTests
{
    private readonly IOptionService _optionService;

    public PluginOptionTests(PluginApplicationFixture appFixture) : base(appFixture)
    {
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
    }

    [IntegrationFact]
    public void RegisterOption_CheckPluginOptionExist_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PluginExampleOption1);
        _ = nameof(PluginExamplePlugin.ConfigureWebApplication);

        //Act
        var result = _optionService.GetOption<PluginExampleOption1>();

        //Assert
        result.Value.Should().Be("200");
    }

    [IntegrationFact]
    public void SetConstOption_HasInInitialSiteData_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PluginConstOption2);
        _ = nameof(PluginExamplePlugin.ConfigureWebApplication);

        //Act
        var result = _optionService.GetOptionsForInitialSiteData();

        //Assert
        _optionService.GetConstOption<PluginConstOption2>().Value.Should().Be("222");
        result.FirstOrDefault(s => s.Type == typeof(PluginConstOption2).FullName).Should().NotBeNull();
    }

}
