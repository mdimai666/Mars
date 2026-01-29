using System.Net;
using System.Net.Sockets;
using FluentAssertions.Common;
using Flurl.Http;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Interfaces;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Mars.E2E.Tests.Fixtures;

public class E2EServerFixture : IAsyncLifetime
{
    protected string? SkipTest => TestConstants.SkipTest;
    public virtual IDatabaseFixture DbFixture { get; } = new DatabaseFixture();

    public IServiceProvider ServiceProvider => _app.Services;
    public IConfigurationRoot Configuration => (IConfigurationRoot)_app.Configuration;

    private WebApplication _app = default!;
    private TokenGenerator _tokenGenerator = new();
    private string? _bearerTokenValue;
    public string BearerTokenValue => _bearerTokenValue ??= _tokenGenerator.GenerateTokenWithClaims();
    public string BearerToken => $"{JwtBearerDefaults.AuthenticationScheme} {BearerTokenValue}";

    public string BaseUrl { get; }

    public MarsDbContext MarsDbContext() => ServiceProvider.GetRequiredService<MarsDbContext>();
    public IFlurlClient GetClient(bool isAnonymous = false)
        => isAnonymous
            ? new FlurlClient(BaseUrl)
            : new FlurlClient(BaseUrl).WithOAuthBearerToken(BearerTokenValue);

    public E2EServerFixture()
    {
        var port = GetFreePort();
        BaseUrl = $"http://localhost:{port}";
    }

    private async Task SetupAppFactory()
    {
        var contentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Mars.WebApp"));
        var wwwRoot = Path.Combine(contentRootPath, "wwwroot");
        var options = new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(Program).Assembly.GetName().Name!,
            EnvironmentName = "Test",
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = wwwRoot
        };

        var builder = WebApplication.CreateBuilder(options);
        builder.Configuration.AddInMemoryCollection([
                                    new ("ConnectionStrings:DefaultConnection", DbFixture.ConnectionString )
                                ]);

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        MarsWebAppStartup.ConfigureBuilder(builder, []);

        builder.Services.Replace(ServiceDescriptor.KeyedSingleton<IFileStorage, InMemoryFileStorage>("data"));
        builder.Services.Replace(ServiceDescriptor.Singleton<IKeyMaterialService, TestKeyMaterialService>(sp => _tokenGenerator.KeyMaterialService));
        builder.Services.AddSingleton<IPluginManagerWrapperForTests, PluginManagerWrapperForTests>();

        var app = builder.Build();
        await MarsWebAppStartup.ConfigureApp(app, builder, []);
        app.Urls.Add(BaseUrl);

        var env = builder.Environment;// app.Services.GetRequiredService<IWebHostEnvironment>();
        var files = env.WebRootFileProvider.GetDirectoryContents("/");
        var count = files.Count(); //Это удобно для отслеживания подключения всех нужных файлов. Сейчас их должно быть 11.

        _app = app;

        await app.StartAsync();
    }

    public async Task InitializeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        await Task.WhenAll([DbFixture.InitializeAsync()]);

        await SetupAppFactory();
        await Task.Delay(1000);
    }

    public async Task DisposeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();

        await Task.WhenAll([DbFixture.DisposeAsync()]);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void ResetMocks()
    {

    }

    public async Task Seed()
    {
        using var scope = ServiceProvider.CreateScope();
        var ef = ServiceProvider.GetRequiredService<IMarsDbContextFactory>().CreateInstance();
        ef.ChangeTracker.Clear();
        var logger = ServiceProvider.GetRequiredService<ILogger<Program>>();
        MarsStartupPartMigrations.SeedData(ServiceProvider, Configuration, logger, true);

        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = UserConstants.TestUser;
        await userRepo.Create(new CreateUserQuery
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = ["Admin"],
            Password = UserConstants.TestUserPassword,
            UserName = user.UserName,
            Id = user.Id,
            AvatarUrl = user.AvatarUrl,

            Type = user.Type,
            MetaValues = [],
        }, default);

        EntitiesCustomize.PostTypeDict = await ef.PostTypes.AsNoTracking().ToDictionaryAsync(s => s.TypeName);
        EntitiesCustomize.UserTypeDict = await ef.UserTypes.AsNoTracking().ToDictionaryAsync(s => s.TypeName);
        ef.ChangeTracker.Clear();

        if (EntitiesCustomize.PostTypeDict.Count == 0 || EntitiesCustomize.UserTypeDict.Count == 0)
        {
            throw new InvalidOperationException("PostTypeDict or UserTypeDict is empty after seeding data");
        }

        ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();
    }

    public async Task WarmupRenderer()
    {
        var af = StartupFront.AppProvider.FirstApp;
        //var renderEngine = af.Features.Get<IWebRenderEngine>();
        //renderEngine.RenderPage()
        //var pageRenderService = ServiceProvider.GetRequiredService<IPageRenderService>();
        //pageRenderService.RenderUrl("/")
        var tsv = af.Features.Get<IWebTemplateService>();
        var processor = new WebSiteRequestProcessor(ServiceProvider, tsv.Template);
        await processor.RenderPage(af, new WebClientRequest(new Uri(BaseUrl)), tsv.Template.IndexPage, new(), default);
    }
}
