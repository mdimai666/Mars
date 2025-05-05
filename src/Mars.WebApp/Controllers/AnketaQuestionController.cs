using System;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnketaQuestionController : StandartController<AnketaQuestionService, AnketaQuestion>
{

    public AnketaQuestionController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override Task<ActionResult<UserActionResult<AnketaQuestion>>> Post([FromBody] AnketaQuestion value)
    {
        throw new NotImplementedException();
    }

    [HttpPost(nameof(SendAnswer1))]
    public async Task<ActionResult<UserActionResult>> SendAnswer1(AnketaAnswer answer, [FromServices] AnketaAnswerService anketaAnswerService)
    {
        var add = await anketaAnswerService.Add(answer);

        return new UserActionResult
        {
            Ok = add != null,
            Message = add != null ? "Ваш ответ сохранен" : "Ошибка"
        };
    }
}
