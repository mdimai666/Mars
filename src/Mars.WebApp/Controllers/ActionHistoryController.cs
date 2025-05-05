using System;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActionHistoryController : StandartController<ActionHistoryService, ActionHistory>
{

    public ActionHistoryController(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }

    [NonAction]
    public override Task<ActionResult<UserActionResult>> Delete(Guid id)
    {
        throw new NotImplementedException();
    }

    [NonAction]
    public override Task<ActionResult<UserActionResult<ActionHistory>>> Post([FromBody] ActionHistory value)
    {
        throw new NotImplementedException();
    }

    [NonAction]
    public override Task<ActionResult<UserActionResult<ActionHistory>>> Put(Guid id, [FromBody] ActionHistory value)
    {
        throw new NotImplementedException();
    }

    [HttpGet($"{nameof(ModelHistory)}/{{modelName}}/{{id:guid}}")]
    public async Task<ActionResult<TotalResponse<ActionHistory>>> ModelHistory(string modelName, Guid id, [FromQuery] QueryFilter filter)
    {
        return await modelService.ListTable(filter, s => s.ActionModel == modelName && s.ModelId == id, x => x.User);
    }
}
