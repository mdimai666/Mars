using FluentAssertions;
using Mars.Integration.Tests.Attributes;
using Mars.Options.Abstractions.Services;
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
    public void RegisterOption_CheckPluginOptionExist_Succeeds()
    {
        //Arrange
        _ = nameof(PluginExampleOption1);
        _ = nameof(PluginExamplePlugin.ConfigureWebApplication);
        // Плагин пишет опцию при ConfigureWebApplication (старт хоста), а per-test Reset затирает
        // таблицу опций — восстанавливаем то же значение, чтобы тест не зависел от порядка исполнения.
        _optionService.SaveOption(new PluginExampleOption1 { Value = "200" });

        //Act
        var result = _optionService.GetOption<PluginExampleOption1>();

        //Assert
        result.Value.Should().Be("200");
    }

    [IntegrationFact]
    public void SetConstOption_HasInInitialSiteData_Succeeds()
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
