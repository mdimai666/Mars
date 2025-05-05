using System;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostCategoryController : StandartController<PostCategoryService, PostCategory>
{
    public PostCategoryController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
