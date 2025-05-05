using System;
using AppShared.Models;
using Mars.Host.Controllers;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoEntityTypeController : StandartController<StoEntityTypeService, StoEntityType>
{
    public StoEntityTypeController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
