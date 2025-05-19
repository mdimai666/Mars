using System.Net.Http.Headers;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Controllers.Schedulers;
using Mars.Integration.Tests.TestControllers;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Mars.UseStartup.MarsParts;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mars.Integration.Tests.Common;

public class ApplicationFixture : IAsyncLifetime
{
    protected string? SkipTest => TestConstants.SkipTest;

    public readonly DatabaseFixture DbFixture = new();

    private HttpClient _authClient = default!;
    private HttpClient _nonAuthClient = default!;

    private WebApplicationFactory<Program> ApplicationFactory = default!;

    public IServiceProvider ServiceProvider => ApplicationFactory.Services;

    public IConfigurationRoot Configuration = default!;

    private static TokenGenerator _tokenGenerator = new TokenGenerator();

    private static string? s_bearerToken;
    public static string BearerToken => s_bearerToken ??= $"{JwtBearerDefaults.AuthenticationScheme} {_tokenGenerator.GenerateTokenWithClaims()}";

    public async Task InitializeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        await SetupAppFactory();
        AddHttpClients();

    }

    public async Task DisposeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        if (ApplicationFactory is not null)
        {
            await ApplicationFactory.DisposeAsync();
        }

        await Task.WhenAll([DbFixture.DisposeAsync()]);
    }

    public HttpClient GetClientEx(bool isAnonymous = false) => isAnonymous ? _nonAuthClient : _authClient;
    public IFlurlClient GetClient(bool isAnonymous = false) => new FlurlClient(GetClientEx(isAnonymous));

    public MarsDbContext MarsDbContext() => ServiceProvider.GetRequiredService<MarsDbContext>();

    private async Task SetupAppFactory()
    {
        await Task.WhenAll([DbFixture.InitializeAsync()]);
        
        ApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.UseEnvironment("Test");
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
                var configurationBuilder = new ConfigurationBuilder()
                                    .AddInMemoryCollection([
                                        new ("ConnectionStrings:DefaultConnection", DbFixture.connectionString )
                                    ]);
                ModifyConfigurationBuilder(configurationBuilder);
                Configuration = configurationBuilder.Build();

                ModifyConfiguration(Configuration);
                builder.UseConfiguration(Configuration);

                builder.ConfigureTestServices(
                    services =>
                    {
                        services.AddLogging();
                        services.AddSingleton(DbFixture._dbContextOptions);

                        //add test controllers
                        services.AddControllers().AddApplicationPart(typeof(TestApi1Controller).Assembly);

                        //services.AddScoped<IMarsDbContext>(sp => DbFixture.DbContext);

                        //services.Replace(ServiceDescriptor.Singleton<IFileStorage>(x => ExternalServiceMock));
                        //services.Replace(ServiceDescriptor.Singleton<IFileStorage, InMemoryFileStorage>()); нельзя заменить из-за ImageProcessor для Media он записывает картинки и тесты ломаются, а IFileStorage плохо поддерживает StreamWritter
                        services.Replace(ServiceDescriptor.KeyedSingleton<IFileStorage, InMemoryFileStorage>("data"));

                        services.AddSingleton(NSubstitute.Substitute.For<ITestDummyTriggerService>());
                    });

                //builder.Configure(app => если использовать то все проподает и 404
                //{
                //});
            });
    }

    protected virtual void ModifyConfigurationBuilder(IConfigurationBuilder builder)
    {
    }

    protected virtual void ModifyConfiguration(IConfigurationRoot configuration)
    {
    }

    private void AddHttpClients()
    {
        var clientOptions = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        _nonAuthClient = ApplicationFactory.CreateClient(clientOptions);
        _authClient = ApplicationFactory.CreateClient(clientOptions);

        var tokenString = _tokenGenerator.GenerateTokenWithClaims();
        _authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenString);
    }

    private void ResetClients()
    {
        _nonAuthClient?.Dispose();
        _authClient?.Dispose();
        AddHttpClients();
    }

    public void ResetMocks()
    {
        MarsDbContext().ChangeTracker.Clear(); //use DbContrext is Pool like .AddDbContextPool()
        ResetClients();
        //ApiClientMock = Substitute.For<IApiClient>();

    }

    public async Task Seed()
    {
        MarsDbContext().ChangeTracker.Clear();
        var logger = ServiceProvider.GetRequiredService<ILogger<Program>>();
        MarsStartupPartMigrations.SeedData(ApplicationFactory.Services, Configuration, logger, true);

        var userRepo = ServiceProvider.GetRequiredService<IUserRepository>();
        var user = UserConstants.TestUser;
        await userRepo.Create(new CreateUserQuery
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = ["Admin"],
            Password = UserConstants.TestUserPassword,
            UserName = user.UserName,
            Id = user.Id
        }, CancellationToken.None);

        EntitiesCustomize.PostTypeDict = await DbFixture.DbContext.PostTypes.ToDictionaryAsync(s => s.TypeName);

        ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

    }

}
