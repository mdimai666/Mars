using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Services;

internal class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserManager _userManager;
    private readonly IUserRepository _userRepository;
    private bool _init = false;
    private RequestContextUser? _user = default;
    private HashSet<string>? _roles = default;

    public RequestContext(IHttpContextAccessor httpContextAccessor, IUserManager userManager, IUserRepository userRepository, ILogger<RequestContext> logger)
    {
        //logger.LogWarning(">ctor!!!");
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _userRepository = userRepository;
        //if (IsAuthenticated)
        //{

        //    //var user = userManager.FindByNameAsync(UserName).ConfigureAwait(false).GetAwaiter().GetResult();
        //    //var user = userRepository.GetDetailByUserName(UserName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        //    //var roles = userManager.GetRolesAsync(use)

        //    //User = 
        //    //var id = _httpContext.User.Identity.
        //}
        //else
        //{
        //    _init = true;
        //}
    }

    (RequestContextUser? user, HashSet<string>? roles) GetData()
    {
        if (_init) return (_user, _roles);
        _init = true;
        if (!IsAuthenticated) return (_user, _roles);

        var user = _userRepository.GetDetailByUserName(UserName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        if (user is not null)
        {
            _user = ToMap(user);
            _roles = user.Roles.ToHashSet();
        }

        return (_user, _roles);
    }

    public ClaimsPrincipal Claims => _httpContextAccessor.HttpContext.User;

    public string Jwt => _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();

    public string UserName => _httpContextAccessor.HttpContext.User.Identity?.Name ?? null!;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;

    public HashSet<string>? Roles => GetData().roles;
    //{
    //    get
    //    {
    //        var roles = new HashSet<string>();
    //        if (_httpContext.User.Identity.IsAuthenticated)
    //        {
    //            var claimsIdentity = _httpContext.User.Identity as ClaimsIdentity;
    //            if (claimsIdentity != null)
    //            {
    //                roles.UnionWith(claimsIdentity.FindAll(ClaimTypes.Role).Select(x => x.Value));
    //            }
    //        }
    //        return roles;
    //    }
    //}

    public RequestContextUser? User => GetData().user;
    //{
    //    get
    //    {
    //        if (_httpContext.User.Identity.IsAuthenticated)
    //        {
    //            return new RequestContextUser
    //            {
    //                Id = Guid.Parse(_httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
    //                                CultureInfo.InvariantCulture),
    //                Email = _httpContext.User.FindFirstValue(ClaimTypes.Email),
    //                PhoneNumber = _httpContext.User.FindFirstValue(ClaimTypes.PhoneNumber),
    //                UserName = _httpContext.User.Identity.Name,
    //                NormalizedUserName = _httpContext.User.FindFirstValue(ClaimTypes.NormalizedUserName),
    //                FullName = _httpContext.User.FindFirstValue(ClaimTypes.GivenName),
    //                PictureUrl = _httpContext.User.FindFirstValue(ClaimTypes.Picture),
    //                Roles = Roles
    //            };
    //        }
    //        return null;
    //    }
    //}

    private RequestContextUser ToMap(UserDetail user)
        => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            BirthDate = user.BirthDate,
            Email = user.Email,
            Gender = user.Gender,
            PhoneNumber = user.PhoneNumber,
            UserName = user.UserName,
        };
}
