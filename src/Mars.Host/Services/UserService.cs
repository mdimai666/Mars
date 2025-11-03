using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Auth;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.Roles;
using Mars.Host.Shared.Mappings.Users;
using Mars.Host.Shared.Mappings.UserTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Services;

internal class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserTypeRepository _userTypeRepository;
    private readonly IValidatorFabric _validatorFabric;
    private readonly IOptionService _optionService;
    private readonly IEventManager _eventManager;
    private readonly INotifyService _notifyService;

    public UserService(IUserRepository userRepository,
                        IRoleRepository roleRepository,
                        IUserTypeRepository userTypeRepository,
                        IValidatorFabric validatorFabric,
                        IOptionService optionService,
                        IEventManager eventManager,
                        INotifyService notifyService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userTypeRepository = userTypeRepository;
        _validatorFabric = validatorFabric;
        _optionService = optionService;
        _eventManager = eventManager;
        _notifyService = notifyService;
    }

    public Task<UserSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _userRepository.Get(id, cancellationToken);

    public Task<UserDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _userRepository.GetDetail(id, cancellationToken);

    public Task<ListDataResult<UserSummary>> List(ListUserQuery query, CancellationToken cancellationToken)
        => _userRepository.List(query, cancellationToken);

    public Task<PagingResult<UserSummary>> ListTable(ListUserQuery query, CancellationToken cancellationToken)
        => _userRepository.ListTable(query, cancellationToken);

    public Task<ListDataResult<UserDetail>> ListDetail(ListUserQuery query, CancellationToken cancellationToken)
        => _userRepository.ListDetail(query, cancellationToken);

    public Task<PagingResult<UserDetail>> ListTableDetail(ListUserQuery query, CancellationToken cancellationToken)
        => _userRepository.ListTableDetail(query, cancellationToken);

    public async Task<UserListEditViewModel> UsersEditViewModel(ListUserQuery query, CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListDetail(query, cancellationToken);
        var roles = await _roleRepository.ListAll(cancellationToken);

        return new UserListEditViewModel
        {
            Users = users,
            Roles = roles,
            //DefaultSelectRole = roles.FirstOrDefault(s => s.Name == "Viewer")?.Id
            DefaultSelectRole = null
        };
    }

    public async Task<UserDetail> Create(CreateUserQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var createdId = await _userRepository.Create(query, cancellationToken);
        var user = (await _userRepository.GetDetail(createdId, cancellationToken))!;

        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.UserAdd(), user);//TODO: сделать явный тип.
            _eventManager.TriggerEvent(payload);
        }

        return user;
    }

    public async Task<UserDetail> Update(UpdateUserQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        //await _validatorFabric.ValidateAndThrowAsync<UpdatePostQueryValidator, UpdatePostQuery>(query, cancellationToken);

        await _userRepository.Update(query, cancellationToken);
        var updated = (await GetDetail(query.Id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.UserUpdate(), updated!);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _userRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.UserDelete(), user!);
        _eventManager.TriggerEvent(payload);

        return UserActionResult.Success();
    }

    #region EDIT_MODEL
    public async Task<UserEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserEditDetail(id, cancellationToken) ?? throw new NotFoundException();

        if (user.MetaValues.Count != user.UserTypeDetail.MetaFields.Count)
        {
            user = user with { MetaValues = PostService.EnrichWithBlankMetaValuesFromMetaValues(user.MetaValues, user.UserTypeDetail.MetaFields) };
        }

        var availRoles = await _roleRepository.ListAll(cancellationToken);

        return new()
        {
            User = user.ToResponse(),
            UserType = user.UserTypeDetail.ToResponse(),
            AvailRoles = availRoles.ToResponse()
        };
    }

    public async Task<UserEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken)
    {
        var userType = await _userTypeRepository.GetDetailByName(type, cancellationToken) ?? throw new NotFoundException();

        var user = GetUserBlank(userType);

        if (user.MetaValues.Count != userType.MetaFields.Count)
        {
            user = user with { MetaValues = PostService.EnrichWithBlankMetaValuesFromMetaValues(user.MetaValues, userType.MetaFields) };
        }

        var availRoles = await _roleRepository.ListAll(cancellationToken);

        return new()
        {
            User = user.ToResponse(),
            UserType = userType.ToResponse(),
            AvailRoles = availRoles.ToResponse()
        };
    }

    public UserEditDetail GetUserBlank(UserTypeDetail userType)
    {
        //var defaultRoles = _optionService.SysOption.Default_Role; //TODO: setup in options

        return new()
        {
            Id = Guid.Empty,
            Email = "",
            UserName = "",
            FirstName = "",
            LastName = "",
            MiddleName = "",
            Type = userType.TypeName,
            Roles = [],
            Gender = UserGender.None,
            ModifiedAt = null,
            PhoneNumber = "",

            BirthDate = null,
            CreatedAt = DateTimeOffset.UtcNow,
            MetaValues = [],
            UserTypeDetail = userType
        };
    }
    #endregion

    public async Task<CreateUserQuery> EnrichQuery(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userType = await _userTypeRepository.GetDetailByName(request.Type, cancellationToken);

        var createQuery = request.ToQuery(userType.MetaFields.ToDictionary(s => s.Id));
        return createQuery;
    }

    public async Task<UpdateUserQuery> EnrichQuery(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var userType = await _userTypeRepository.GetDetailByName(request.Type, cancellationToken);

        var createQuery = request.ToQuery(userType.MetaFields.ToDictionary(s => s.Id));
        return createQuery;
    }

    public async Task<UserProfileInfoDto?> UserProfileInfo(Guid id, CancellationToken cancellationToken)
    {
        var userDetail = await _userRepository.GetDetail(id, cancellationToken);
        if (userDetail is null) return null;

        int commentCount = 0;

        return userDetail.ToProfile(commentCount);
    }

    public Task<UserEditProfileDto?> UserEditProfileGet(Guid id, CancellationToken cancellationToken)
    {
        return _userRepository.UserEditProfileGet(id, cancellationToken);
    }

    //public async Task<UserEditProfileDto>> UserEditProfileForAdminGet(Guid id)
    //{
    //    return new UserEditProfileDto();
    //}

    public Task<UserActionResult<UserEditProfileDto>> UserEditProfileUpdate(UserEditProfileDto profile, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //var ef = GetEFContext();

        //var user = ef.Users.Include(s => s.MetaValues).ThenInclude(s => s.MetaField).FirstOrDefault(s => s.Id == profile.Id);

        //user.FirstName = profile.FirstName;
        //user.LastName = profile.LastName;
        //user.MiddleName = profile.MiddleName;
        //user.Email = profile.Email;
        //user.PhoneNumber = profile.Phone;
        //user.Gender = profile.Gender;

        //user.BirthDate = profile.BirthDate;
        //user.AvatarUrl = profile.AvatarUrl;

        //user.About = profile.About;

        //user.GeoRegionId = profile.GeoRegionId;
        //user.GeoMunicipalityId = profile.GeoMunicipalityId;
        //user.GeoLocationId = profile.GeoLocationId;

        //var metaFields = UserMetaFields(ef);

        //await UpdateUserMetaValues(ef, user.Id, user.MetaValues, profile.MetaValues, metaFields);

        //ef.SaveChanges();

        //UserManager<User> um = _serviceProvider.GetRequiredService<UserManager<User>>();
        //await um.UpdateSecurityStampAsync(user.CopyViaJsonConversion<User>()).ConfigureAwait(false);

        //user = ef.Users.Include(s => s.MetaValues).ThenInclude(s => s.MetaField).FirstOrDefault(s => s.Id == profile.Id);
        //user.MetaValues = MetaField.GetValuesBlank(user.MetaValues, metaFields);

        //user.MetaFields = metaFields;

        //return new UserActionResult<UserEditProfileDto>
        //{
        //    Ok = true,
        //    Data = new UserEditProfileDto(user),
        //    Message = "Успешно сохранено"
        //};

    }

    public Task<UserActionResult> UpdateUserRoles(Guid id, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        return _userRepository.UpdateUserRoles(id, roles, cancellationToken);
    }

    public async Task<UserActionResult> SetPassword(SetUserPasswordQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _userRepository.SetPassword(query, cancellationToken);
    }

    public async Task<UserActionResult> SetPassword(SetUserPasswordByIdQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _userRepository.SetPassword(query, cancellationToken);
    }

    public Task<UserActionResult> SendInvation(Guid userId, CancellationToken cancellationToken)
    {
        return _notifyService.SendNotify_Invation(userId, cancellationToken);
    }

    public Task<AuthorizedUserInformationDto?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _userRepository.FindByEmailAsync(email, cancellationToken);
    }

    public async Task<AuthorizedUserInformationDto> RemoteUserUpsert(UpsertUserRemoteDataQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        return await _userRepository.RemoteUserUpsert(query, cancellationToken);
    }
}
