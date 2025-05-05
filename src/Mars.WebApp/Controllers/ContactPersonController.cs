using System;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class ContactPersonController : StandartController<ContactPersonService, ContactPerson>
{

    public ContactPersonController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [AllowAnonymous]
    public override Task<ActionResult<TotalResponse<ContactPerson>>> ListTable(QueryFilter filter)
    {
        return base.ListTable(filter);
    }
}
