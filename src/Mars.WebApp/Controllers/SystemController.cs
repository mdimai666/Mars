using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Systems;
using Mars.UseStartup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]/[action]")] //TODO: Префикс добавить что ли (/api/s/). Обсудить с алексеем
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[AllExceptionCatchToUserActionResultFilter]
public class SystemController : ControllerBase
{

    private readonly IMarsSystemService _marsSystemService;

    public SystemController(IMarsSystemService marsSystemService)
    {
        _marsSystemService = marsSystemService;
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public string HealthCheck()
    {
        return "OK";
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public string HealthCheck2()
    {
        return "StartDateTime - " + MarsStartupInfo.StartDateTime.ToString("dd.MM.yyyy dd:mm:ss zz");
    }

    [HttpGet]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public string MemoryUsage()
        => _marsSystemService.MemoryUsage();



    [HttpGet]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public string AppUptime()
        => _marsSystemService.AppUptime();

    [HttpGet]
    public DateTimeOffset AppStartDateTime()
        => _marsSystemService.AppStartDateTime();

    [HttpGet]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public string SystemUptime()
        => _marsSystemService.SystemUptime();

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public long SystemUptimeMillis()
        => _marsSystemService.SystemUptimeMillis();

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public SystemMinStatResponse SystemMinStat()
        => _marsSystemService.SystemMinStat();

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IEnumerable<KeyValuePair<string, string>> AboutSystem()
        => _marsSystemService.AboutSystem();


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IEnumerable<KeyValuePair<string, string>> HostCacheSettings()
        => _marsSystemService.HostCacheSettings();

}
