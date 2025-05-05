using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.Users;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<UserDetailResponse?> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _userService.GetDetail(id, cancellationToken))?.ToResponse()
                    ?? throw new NotFoundException();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<UserDetailResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userService.Create(request.ToQuery(), cancellationToken);
        return Created("{id}", created.ToResponse());
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<UserDetailResponse> Update([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.Update(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.Delete(id, cancellationToken);
        if (result.Ok) return Ok();
        return NotFound(null);
    }

    [HttpGet]
    public async Task<ListDataResult<UserListItemResponse>> List([FromQuery] ListUserQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTable")]
    public async Task<PagingResult<UserListItemResponse>> ListTable([FromQuery] TableUserQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("ListDetail")]
    public async Task<ListDataResult<UserDetailResponse>> ListDetail([FromQuery] ListUserQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.ListDetail(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("ListTableDetail")]
    public async Task<PagingResult<UserDetailResponse>> ListTableDetail([FromQuery] TableUserQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.ListTableDetail(request.ToQuery(), cancellationToken)).ToResponse();
    }

    //-----------------

    [HttpGet("UsersEditViewModel")]
    public async Task<UserListEditViewModelResponse> UsersEditViewModel([FromQuery] ListUserQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.UsersEditViewModel(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("UserProfileInfo/{id:guid}")]
    public async Task<UserProfileInfoResponse?> UserProfileInfo(Guid id, CancellationToken cancellationToken)
    {
        return (await _userService.UserProfileInfo(id, cancellationToken))?.ToResponse();
    }

    [HttpGet("UserEditProfile/{id:guid}")]
    public async Task<UserEditProfileResponse?> UserEditProfile(Guid id, CancellationToken cancellationToken)
    {
        return (await _userService.UserEditProfileGet(id, cancellationToken))?.ToResponse();
    }

    [HttpPut("UserEditProfile/{id:guid}")]
    public async Task<UserActionResult<UserEditProfileResponse>> UserEditProfile_Update(UserEditProfileDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.UserEditProfileUpdate(dto, cancellationToken);

        return new UserActionResult<UserEditProfileResponse>
        {
            Ok = result.Ok,
            Message = result.Message,
            Data = result.Data.ToResponse()
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("UpdateUserRoles/{id:guid}")]
    public async Task<UserActionResult> UpdateUserRoles(Guid id, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        return await _userService.UpdateUserRoles(id, roles, cancellationToken);
    }

    //[Authorize(Roles = "Admin")]
    //[HttpPut("SetUserState/{id:guid}")]
    //public async Task<UserActionResult> SetUserState(Guid id, IEnumerable<bool> state)
    //{
    //    throw new NotImplementedException();
    //    //return modelService.SetUserState(id, state.ElementAt(0));
    //}


    [Authorize(Roles = "Admin")]
    [HttpPut("SetPassword")]
    public async Task<UserActionResult> SetPassword(SetUserPasswordByIdRequest request, CancellationToken cancellationToken)
    {
        return await _userService.SetPassword(request.ToQuery(), cancellationToken);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("SendInvation/{id:guid}")]
    public async Task<UserActionResult> SendInvation(Guid id, CancellationToken cancellationToken)
    {
        return await _userService.SendInvation(id, cancellationToken);
    }


    [Authorize(Roles = "Admin")]
    [HttpGet("UserMetaFields")]
    public Task<List<MetaFieldResponse>> UserMetaFields()
    {
        throw new NotImplementedException();
        //return _userService.UserMetaFields(ef);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost("UserMetaFields")]
    public async Task<IReadOnlyCollection<MetaFieldResponse>> UserMetaFields(IReadOnlyCollection<MetaFieldDto> metaFields)
    {
        return (await _userService.UserMetaFields(metaFields)).ToResponse();
    }
}
