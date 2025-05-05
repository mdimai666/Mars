using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationTypeController : StandartController<GeoLocationTypeService, GeoLocationType>
{
    public GeoLocationTypeController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
