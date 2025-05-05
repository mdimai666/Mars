using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnketaAnswerController : StandartController<AnketaAnswerService, AnketaAnswer>
{

    public AnketaAnswerController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async override Task<ActionResult<TotalResponse<AnketaAnswer>>> ListTable([NotNull] QueryFilter filter)
    {
        return await modelService.ListTable(filter, null, s => s.User);
    }
}
