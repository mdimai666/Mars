using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PluginExample.Data;
using PluginExample.Data.Entities;

namespace Mars.Plugin.Integration.Tests.Tests.Data;

public class PluginDbContextTests : BasePluginTests
{
    public PluginDbContextTests(PluginApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task SeededData_ObjectsExists_Success()
    {
        //Arrange
        _ = nameof(MyPluginDbContext);
        using var ef = AppFixture.ServiceProvider.GetRequiredService<MyPluginDbContext>();

        //Act
        var data = await ef.News.CountAsync();

        //Assert
        data.Should().BeGreaterThan(0);
    }

    [IntegrationFact]
    public async Task DbContext_ListDataCrossModelsWithMarsUser_Success()
    {
        //Arrange
        _ = nameof(MyPluginDbContext);
        _ = nameof(PluginNewsEntity);
        using var ef = AppFixture.ServiceProvider.GetRequiredService<MyPluginDbContext>();
        var user = await ef.Users.FirstAsync();
        var created = Enumerable.Range(0, 3).Select(i => new PluginNewsEntity
        {
            Content = "x",
            Title = $"title - {i}",
            UserId = user.Id,
        });

        //Act
        ef.News.AddRange(created);
        await ef.SaveChangesAsync();

        //Assert
        ef.ChangeTracker.Clear();
        var savedNews = await ef.News.Include(s => s.User).Where(s => s.Content == "x").ToListAsync();
        savedNews.Count.Should().Be(created.Count());
        savedNews.Should().AllSatisfy(x => x.User.Should().NotBeNull());
    }
}
