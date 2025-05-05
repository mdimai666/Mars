using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoRegionController : StandartController<GeoRegionService, GeoRegion>
{
    public GeoRegionController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
