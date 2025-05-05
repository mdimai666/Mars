using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoMunicipalityController : StandartController<GeoMunicipalityService, GeoMunicipality>
{
    public GeoMunicipalityController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
