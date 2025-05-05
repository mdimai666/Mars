using Microsoft.AspNetCore.Mvc;

namespace PluginExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyPluginController : ControllerBase
{
    [HttpGet("PluginEndpoint")]
    public string PluginEndpoint()
    {
        return "OK";
    }
}
