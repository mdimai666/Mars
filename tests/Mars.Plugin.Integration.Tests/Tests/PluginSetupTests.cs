using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using PluginExample;
using PluginExample.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginSetupTests : BasePluginTests
{
    public PluginSetupTests(PluginApplicationFixture appFixture) : base(appFixture)
    {

    }

    [IntegrationFact]
    public void Setup_PluginIncluded_ShouldExist()
    {
        //Arrange
        _ = nameof(PluginExamplePlugin);
        var expectPluginAssembly = typeof(PluginExamplePlugin).Assembly;

        //Act
        var plugin = PluginManager.Plugins.FirstOrDefault(s =>
        {
            return s.Info.AssemblyFullName == expectPluginAssembly.FullName;
        });

        //Assert
        plugin.Should().NotBeNull();
        using var ef = AppFixture.ServiceProvider.GetRequiredService<MyPluginDbContext>();

        //check migrations work
        ef.Database.GetMigrations().Should().HaveCount(1);
        ef.Database.GetPendingMigrations().Should().HaveCount(1);
        ef.Database.GetAppliedMigrations().Should().HaveCount(0);
        ef.News.Should().HaveCount(1);
        ef.Posts.Should().HaveCountGreaterThan(0);
        ef.Users.Should().HaveCountGreaterThan(0);
    }
}
