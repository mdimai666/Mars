using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController : StandartController<CommentService, Comment>
{

    public CommentController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
