using FluentAssertions;
using Mars.Integration.Tests.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PluginExample;
using PluginExample.Data;

namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginSetupTests : BasePluginTests
{
    public PluginSetupTests(PluginApplicationFixture appFixture) : base(appFixture)
    {

    }

    [IntegrationFact]
    public async Task Setup_PluginIncluded_ShouldExist()
    {
        //Arrange
        _ = nameof(PluginExamplePlugin);
        _ = nameof(PluginExamplePlugin.ApplyMigrations);
        var expectPluginAssembly = typeof(PluginExamplePlugin).Assembly;

        //Act
        var plugin = PluginManager.Plugins.Single(s =>
        {
            return s.Info.AssemblyFullName == expectPluginAssembly.FullName;
        });

        //Assert
        plugin.Should().NotBeNull();
        var ef = AppFixture.ServiceProvider.GetRequiredService<MyPluginDbContext>();

        //check migrations work
        ef.Database.GetMigrations().Should().HaveCount(1);
        ef.Database.GetPendingMigrations().Should().HaveCount(0);
        ef.Database.GetAppliedMigrations().Should().HaveCount(1);
        ef.Posts.Should().HaveCountGreaterThan(0);
        ef.Users.Should().HaveCountGreaterThan(0);

        var news = await ef.News.Include(s => s.User).ToListAsync();
        news.Should().HaveCount(1);
        news.First().User.Should().NotBeNull();
        var firstUser = await ef.Users.OrderBy(s => s.UserName).FirstAsync();
        news.First().User.UserName.Should().Be(firstUser.UserName);
    }
}
