using System.Linq.Expressions;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Data;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Templators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Mars.Host.Shared.Mappings.Users;
using Mars.Host.Shared.Managers;
using Mars.Shared.Common;
using Mars.Host.Shared.Dto.Auth;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Validators;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Dto.Users.Passwords;
namespace Mars.Host.Services;

internal class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IValidatorFabric _validatorFabric;
    private readonly IEventManager _eventManager;
    private readonly INotifyService _notifyService;

    public UserService(IUserRepository userRepository,
                        IRoleRepository roleRepository,
                        IValidatorFabric validatorFabric,
                        IEventManager eventManager,
                        INotifyService notifyService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _validatorFabric = validatorFabric;
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

    //public async Task<List<UserRoleDto>> UserWithRoles(Expression<Func<User, bool>> predicate = null)
    //{
    //    using (var context = GetEFContext())
    //    {
    //        var userroles = await context.UserRoles
    //            .AsNoTracking()
    //            .ToListAsync();

    //        IQueryable<User> usersQuery = context.Users;

    //        if (predicate is not null) usersQuery = usersQuery.Where(predicate);

    //        var users = await usersQuery.AsNoTracking().Where(s => s.UserName != "empty").ToListAsync();
    //        var roles = await context.Roles.AsNoTracking().ToListAsync();

    //        List<UserRoleDto> list = new();

    //        var grouped = userroles.GroupBy(s => s.UserId).ToList();

    //        foreach (var user in users)
    //        {
    //            var group = grouped.FirstOrDefault(s => s.Key == user.Id);

    //            if (group is not null)
    //            {
    //                var rolesIds = group.ToList().Select(s => s.RoleId).ToList();
    //                var _roles = roles.Where(s => rolesIds.Contains(s.Id)).ToList();
    //                list.Add(new UserRoleDto(user, _roles));
    //            }
    //            else
    //            {
    //                list.Add(new UserRoleDto(user, new List<Role>()));
    //            }
    //        }

    //        return list;
    //    }
    //}

    //public async Task<UserRoleDto> UserWithRolesOne(Guid userId)
    //{
    //    using var ef = GetEFContext();
    //    return await UserWithRolesOne(ef, userId);
    //}

    //public async Task<UserRoleDto?> UserWithRolesOne(IMarsDbContext ef, Guid userId)
    //{

    //    List<IdentityUserRole<Guid>> userroles = await ef.UserRoles
    //        .AsNoTracking()
    //        .ToListAsync();

    //    User user = ef.Users
    //                    .Include(s => s.MetaValues)
    //                        .ThenInclude(s => s.MetaField)
    //                    .AsNoTracking()
    //                    .FirstOrDefault(s => s.Id == userId);

    //    if (user is null) return null;

    //    MetaFieldService metaFieldService = _serviceProvider.GetRequiredService<MetaFieldService>();

    //    var metaFields = UserMetaFields(ef);

    //    user.MetaFields = metaFields;
    //    user.MetaValues = metaFieldService.GetValuesBlank(user.MetaValues, metaFields);

    //    var roles = await ef.Roles.AsNoTracking().ToListAsync();

    //    UserRoleDto dto;

    //    List<IGrouping<Guid, IdentityUserRole<Guid>>> grouped = userroles.GroupBy(s => s.UserId).ToList();

    //    var group = grouped.FirstOrDefault(s => s.Key == user.Id);

    //    if (group is not null)
    //    {
    //        var rolesIds = group.ToList().Select(s => s.RoleId).ToList();
    //        var _roles = roles.Where(s => rolesIds.Contains(s.Id)).ToList();
    //        dto = new UserRoleDto(user, _roles);
    //    }
    //    else
    //    {
    //        dto = new UserRoleDto(user, new List<Role>());
    //    }

    //    return dto;

    //}

    //public async Task<UserActionResult<User>> CreateUser(CreateUserQuery createQuery, string? username = null, Guid? userId = null, CancellationToken cancellationToken)
    //{
    //    throw new NotImplementedException();
    //}

    public async Task<UserDetail> Create(CreateUserQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var createdId = await _userRepository.Create(query, cancellationToken);
        var user = (await _userRepository.GetDetail(createdId, cancellationToken))!;

        {
            ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.UserAdd(), user);//TODO: сделать явный тип.
            _eventManager.TriggerEvent(payload);
        }

        return user;
    }

    public async Task<UserDetail> Update(UpdateUserQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);
        //await _validatorFabric.ValidateAndThrowAsync<UpdatePostQueryValidator, UpdatePostQuery>(query, cancellationToken);

        await _userRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        {
            ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.UserUpdate(), updated!);
            _eventManager.TriggerEvent(payload);
        }

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await Get(id, cancellationToken) ?? throw new NotFoundException();

        try
        {
            await _userRepository.Delete(id, cancellationToken);

            {
                ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.UserDelete(), user!);
                _eventManager.TriggerEvent(payload);
            }

            return UserActionResult.Success();
        }
        catch (Exception ex)
        {
            return UserActionResult.Exception(ex);
        }
    }

    public async Task<UserProfileInfoDto?> UserProfileInfo(Guid id, CancellationToken cancellationToken)
    {
        //MetaFieldService metaFieldService = _serviceProvider.GetRequiredService<MetaFieldService>();
        //var user = Get(id, s => s.MetaValues).ConfigureAwait(false).GetAwaiter().GetResult();
        //using var ef = GetEFContext();
        //var metaFields = UserMetaFields(ef);
        //user.MetaValues = metaFieldService.GetValuesBlank(user.MetaValues, metaFields);

        //int comments = 0;// ef.Comments.Count(s => s.UserId == id);
        //throw new NotImplementedException();

        //IList<string> roles = _userManager.GetRolesAsync(user.CopyViaJsonConversion<User>()).Result;

        //return new UserProfileInfoDto(user, roles, 0, comments);

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

    public async Task<UserActionResult<UserEditProfileDto>> UserEditProfileUpdate(UserEditProfileDto profile, CancellationToken cancellationToken)
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

    //public async Task<UserActionResult<UserEditProfileDto>> UserEditProfileForAdminUpdate(UserEditProfileForAdminDto profile)
    //{
    //    return new UserActionResult<UserEditProfileDto>();
    //}

    public Task<UserActionResult> UpdateUserRoles(Guid id, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        //var ef = GetEFContext();
        //List<Role> rolesList = await ef.Roles.AsNoTracking().ToListAsync();

        //List<Role> acceptRoles = rolesList.Where(s => roles.Contains(s.Id)).ToList();

        //var user = _userManager.FindByIdAsync(id.ToString()).Result;

        //var newList = acceptRoles.Select(s => s.Name).ToList();

        //var existList = _userManager.GetRolesAsync(user).Result;
        //var requireRemoveList = existList.Where(s => newList.Contains(s) == false);
        //var requireAddList = newList.Where(s => existList.Contains(s) == false);

        //if (requireRemoveList.Count() > 0)
        //    await _userManager.RemoveFromRolesAsync(user, requireRemoveList);
        //if (requireAddList.Count() > 0)
        //    await _userManager.AddToRolesAsync(user, requireAddList);

        //UserManager<User> um = _serviceProvider.GetRequiredService<UserManager<User>>();
        //await um.UpdateSecurityStampAsync(user).ConfigureAwait(false);

        //return new UserActionResult
        //{
        //    Ok = true,
        //    Message = "Успешно сохранено"
        //};

        return _userRepository.UpdateUserRoles(id, roles, cancellationToken);

    }

    //public UserActionResult SetUserState(Guid id, bool state)
    //{
    //    var ef = GetEFContext();

    //    var user = ef.Users.First(s => s.Id == id);
    //    user.LockoutEnabled = state;
    //    ef.SaveChanges();

    //    return new UserActionResult
    //    {
    //        Ok = true,
    //        Message = "Успешно сохранено"
    //    };
    //}

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

    public IReadOnlyCollection<MetaFieldDto> UserMetaFields(object ef)
    {
        //var user = ef.Users
        //        .Include(s => s.MetaFields)
        //        .FirstOrDefault();

        //return user.MetaFields.ToList();
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyCollection<MetaFieldDto>> UserMetaFields(IReadOnlyCollection<MetaFieldDto> entity_MetaFields)
    {
        //using var ef = GetEFContext();

        //var _item = ef.Users
        //         .Include(s => s.MetaFields)
        //         .FirstOrDefault();

        //if (true)
        //{

        //    foreach (var e in entity_MetaFields)
        //    {
        //        if (e.Id == Guid.Empty) e.Id = Guid.NewGuid();
        //    }

        //    //var existList = ef.MetaValues.Where(s => s.PostId == _item.Id).ToList();
        //    var existList = _item.MetaFields;
        //    var existListIds = existList.Select(s => s.Id);
        //    var entityPutMetaValuesIds = entity_MetaFields.Select(s => s.Id);

        //    var requireAddListIds = entityPutMetaValuesIds.Where(s => existListIds.Contains(s) == false);
        //    var requireRemoveListIds = existListIds.Where(s => entityPutMetaValuesIds.Contains(s) == false);

        //    var requireAddList = entity_MetaFields.Where(s => requireAddListIds.Contains(s.Id));
        //    var requireRemoveList = existList.Where(s => requireRemoveListIds.Contains(s.Id));


        //    foreach (var z in entity_MetaFields)
        //    {
        //        //z.PostId = id;
        //        foreach (var f in existList)
        //        {
        //            if (f.Id == z.Id)
        //            {
        //                ef.Entry(f).CurrentValues.SetValues(z);
        //            }
        //        }
        //    }

        //    foreach (var z in requireRemoveList)
        //    {
        //        ef.MetaFields.Remove(z);
        //    }
        //    foreach (var z in requireAddList)
        //    {
        //        ef.MetaFields.Add(z);
        //    }
        //    //ef.Entry(_item).Collection(s=>s.MetaValues).IsModified = false;

        //    await UpdateManyToMany(ef.UserMetaFields, s => s.UserId == _item.Id,
        //        s => s.UserId, s => s.MetaFieldId,
        //        _item.Id, entity_MetaFields.Select(s => s.Id));
        //}

        ////ef.Entry(_item).CurrentValues.SetValues(entity);
        //_item.Modified = DateTime.Now;
        //int state = await ef.SaveChangesAsync();

        //var a = UserMetaFields(ef);

        //return new UserActionResult<List<MetaField>>
        //{
        //    Ok = true,
        //    Data = a,
        //    Message = "Успешно сохранено"
        //};

        throw new NotImplementedException();
    }

    //public async Task UpdateUserMetaValues(MarsDbContextLegacy ef, Guid userId, IReadOnlyCollection<MetaValueDto> fromDbMetaValues, ICollection<MetaValue> entity_MetaValues, ICollection<MetaFieldDto> metaFields)
    //{

    //    //foreach (var e in entity_MetaValues)
    //    //{
    //    //    if (e.Id == Guid.Empty) e.Id = Guid.NewGuid();
    //    //    //e.MetaField = null;
    //    //}

    //    //var ignoreMetaFieldsIds = metaFields.Where(s => s.Disable).Select(s => s.Id);

    //    ////var existList = ef.MetaValues.Where(s => s.PostId == _item.Id).ToList();
    //    //var existList = fromDbMetaValues;
    //    //var existListIds = existList.Select(s => s.Id);
    //    //var entityPutMetaValuesIds = entity_MetaValues.Select(s => s.Id);

    //    //var requireAddListIds = entityPutMetaValuesIds.Where(s => existListIds.Contains(s) == false);
    //    ////var requireRemoveListIds = existList.Where(s => entityPutMetaValuesIds.Contains(s.Id) == false && !ignoreMetaFieldsIds.Contains(s.MetaFieldId)).Select(s=>s.Id);
    //    //var requireRemoveListIds = entity_MetaValues.Where(s => s.MarkForDelete).Select(s => s.Id);

    //    //var requireAddList = entity_MetaValues.Where(s => requireAddListIds.Contains(s.Id));
    //    //var requireRemoveList = existList.Where(s => requireRemoveListIds.Contains(s.Id));


    //    //foreach (var e in entity_MetaValues) e.MetaField = null;
    //    //foreach (var e in requireAddList) e.MetaField = null;
    //    //foreach (var e in requireRemoveList) e.MetaField = null;


    //    //foreach (var z in entity_MetaValues)
    //    //{
    //    //    foreach (var f in existList)
    //    //    {
    //    //        if (f.Id == z.Id)
    //    //        {
    //    //            ef.Entry(f).CurrentValues.SetValues(z);
    //    //        }
    //    //    }
    //    //}
    //    //foreach (var z in requireRemoveList)
    //    //{
    //    //    ef.MetaValues.Remove(z);
    //    //    //_item.MetaValues.Remove(z);
    //    //}
    //    //foreach (var z in requireAddList)
    //    //{
    //    //    ef.MetaValues.Add(z);
    //    //    //_item.MetaValues.Add(z);
    //    //}

    //    //IEnumerable<Guid> newMMList = fromDbMetaValues.Select(s => s.Id).Except(requireRemoveListIds).Concat(requireAddListIds);

    //    //await UpdateManyToMany(ef.UserMetaValues, s => s.UserId == userId,
    //    //    s => s.UserId, s => s.MetaValueId,
    //    //    userId, newMMList);

    //    throw new NotImplementedException();
    //}


    //public JObject AsJson22(object pctx, object userWithMetaValues) => AsJson22(pctx, userWithMetaValues);
    //public JObject AsJson2(MfPreparePostContext pctx, User userWithMetaValues)
    //{

    //    //JsonSerializerOptions opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    //    //{
    //    //    MaxDepth = 0,
    //    //    //IgnoreReadOnlyProperties = true,
    //    //    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,

    //    //};


    //    ////if (pctx.postType.EnabledFeatures.Contains(nameof(Post.Likes)) == true)
    //    ////{
    //    ////    if (pctx.user is not null)
    //    ////    {
    //    ////        postDto.IsLiked = pctx.ef.Posts.Include(s => s.Likes).Count(s => s.Id == pctx.post.Id && s.Likes.Select(s => s.UserId).Contains(pctx.user.Id)) > 0;
    //    ////    }
    //    ////}

    //    //UserDto dto = new UserDto(userWithMetaValues);

    //    //JsonObject userMeta;

    //    //if (userWithMetaValues is not null)
    //    //{
    //    //    userMeta = MfPreparePostContext.AsJson2(ref pctx, userWithMetaValues.MetaValues, pctx.userMetaFields, _serviceProvider);
    //    //}
    //    //else
    //    //{
    //    //    userMeta = new JsonObject();
    //    //}

    //    //var meOpt = new JsonMergeSettings
    //    //{
    //    //    // union array values together to avoid duplicates
    //    //    MergeArrayHandling = MergeArrayHandling.Union
    //    //};

    //    //JObject userDto2 = JObject.FromObject(dto);
    //    //userDto2.Merge(JObject.Parse(userMeta.ToJsonString()), meOpt);



    //    //return userDto2;

    //    throw new NotImplementedException();

    //}
}

