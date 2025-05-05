using System.Security.Claims;
using AppShared.Models;
using Mars.Host.Data;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

public class MinimalService : MinimalService<MarsDbContextLegacy>
{
    public MinimalService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}

public class MinimalService<TDbContext> where TDbContext : MarsDbContextLegacy
{
    readonly protected string _connectionString;
    readonly protected IConfiguration _configuration;
    readonly protected IServiceProvider _serviceProvider;
    readonly protected IHttpContextAccessor _httpContextAccessor;
    readonly protected UserManager<UserEntity> _userManager;
    //readonly protected ILogger _logger;

    protected ClaimsPrincipal _user => _httpContextAccessor.HttpContext.User;


    public MinimalService(
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        //ef = dbContext;
        //UserManager<IdentityUser> userManager,
        //IHttpContextAccessor httpContextAccessor,
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _userManager = serviceProvider.GetRequiredService<UserManager<UserEntity>>();
        _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        _connectionString = _configuration.GetConnectionString("DefaultConnection");

        //ILoggerFactory logger = _serviceProvider.GetRequiredService<ILoggerFactory>();

        //_logger = logger.CreateLogger(GetType());
    }

    protected virtual TDbContext GetEFContext()
    {
        return _serviceProvider.GetRequiredService<TDbContext>();
    }

    protected Guid? _userId
    {
        get
        {
            if (_user.Identity.IsAuthenticated == false) return null;

            string id = _userManager.GetUserId(_user);
            if (Guid.TryParse(id, out Guid guid))
            {
                return guid;
            };
            return null;
        }
    }

    protected UserEntity GetUser()
    {
        return _userManager.GetUserAsync(_user).Result;
    }

}
