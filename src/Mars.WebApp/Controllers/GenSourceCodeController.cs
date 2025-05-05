using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class GenSourceCodeController : ControllerBase
{
    readonly IMetaModelTypesLocator _mlocator;

    public GenSourceCodeController(IMetaModelTypesLocator mlocator)
    {
        _mlocator = mlocator;
    }

    [HttpGet]
    public string MetaModelsMto(string lang = "csharp")
    {
        throw new NotImplementedException();
        //using var ef = serviceProvider.GetService<MarsDbContextLegacy>();

        //var postTypeList = ef.PostTypes
        //                        .Include(s => s.MetaFields)
        //                        //.First(s => s.TypeName == postTypeName);
        //                        .ToList();

        //var userService = serviceProvider.GetService<UserService>();

        //var userMetaFields = userService.UserMetaFields(ef);

        //string code = GenClassMto.GenPostTypesAsMtoString(postTypeList, userMetaFields, _mlocator);

        //return code;
    }
}
