using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Services;

internal class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private bool _init;
    private RequestContextUser? _user;
    private HashSet<string>? _roles;

    public RequestContext(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, ILogger<RequestContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    (RequestContextUser? user, HashSet<string>? roles) GetData()
    {
        if (_init) return (_user, _roles);
        _init = true;
        if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated) return (_user, _roles);

        var user = _userRepository.GetAuthorizedUserInformation(UserName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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

    public bool IsAuthenticated => User is not null;

    public HashSet<string>? Roles => GetData().roles;
    public RequestContextUser? User => GetData().user;

    private RequestContextUser ToMap(AuthorizedUserInformationDto user)
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
            Roles = user.Roles.ToHashSet(),
            AvatarUrl = user.AvatarUrl,
        };
}
