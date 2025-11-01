using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.SSO.Host.OAuth.Controllers;

[Controller]
[Route(".well-known", Name = "oauth")]
public class JwksController : Controller
{
    private readonly IKeyMaterialService _keys;

    public JwksController(IKeyMaterialService keys)
    {
        _keys = keys;
    }

    [HttpGet("jwks.json")]
    public IActionResult GetJwks()
    {
        //var jwk = _keys.GetJwk();
        //return Ok(new
        //{
        //    keys = new[] { jwk }
        //});

        return Ok(_keys.GetJwksJson());
    }
}
