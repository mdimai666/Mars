using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoMunicTypeController : StandartController<GeoMunicTypeService, GeoMunicType>
{
    public GeoMunicTypeController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
