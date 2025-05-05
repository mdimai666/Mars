using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoRegionCenterController : StandartController<GeoRegionCenterService, GeoRegionCenter>
{
    public GeoRegionCenterController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
