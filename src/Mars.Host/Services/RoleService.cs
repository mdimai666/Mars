using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Roles;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;
using Mars.Shared.ViewModels;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Services;

public class RoleService : IRoleService
{
    readonly IStringLocalizer<AppRes> L;
    private readonly IEventManager _eventManager;
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository, IStringLocalizer<AppRes> stringLocalizer, IEventManager eventManager)
    {
        L = stringLocalizer;
        _eventManager = eventManager;
        _roleRepository = roleRepository;
    }

    public Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken)
        => _roleRepository.Get(id, cancellationToken);

    public Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken)
        => _roleRepository.List(query, cancellationToken);

    public Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken)
        => _roleRepository.ListTable(query, cancellationToken);

    public async Task<RoleDetail> Create(CreateRoleQuery query, CancellationToken cancellationToken)
    {
        var id = await _roleRepository.Create(query, cancellationToken);
        var created = await Get(id, cancellationToken);

        ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.RoleAdd(), created!);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created!;
    }

    public async Task<RoleDetail> Update(UpdateRoleQuery query, CancellationToken cancellationToken)
    {
        await _roleRepository.Update(query, cancellationToken);
        var updated = await Get(query.Id, cancellationToken);

        ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.RoleUpdate(), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await Get(id, cancellationToken) ?? throw new NotFoundException();

        try
        {
            await _roleRepository.Delete(id, cancellationToken);

            ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.RoleDelete(), role);
            _eventManager.TriggerEvent(payload);

            return UserActionResult.Success();
        }
        catch (Exception ex)
        {
            return UserActionResult.Exception(ex);
        }
    }

    #region OLD
    List<RoleCapGroup>? _cachedList;

    public List<RoleCapGroup> GetClaimsBlankList()
    {
        return _cachedList ??= GetTypeAsClaimsListGrouped(typeof(RoleCaps));
    }
    public List<RoleCapGroup> GetTypeAsClaimsListGrouped(Type type)
    {

        Type[] internals = type.GetNestedTypes(BindingFlags.Static |
                                                   BindingFlags.Instance |
                                                   BindingFlags.Public);

        List<RoleCapGroup> groups = new();

        foreach (Type model in internals)
        {
            var constants = GetConstants(model);

            List<RoleCapElement> caps = new();

            foreach (var field in constants)
            {
                DisplayAttribute? display = field.GetCustomAttribute<DisplayAttribute>();

                string name = field.Name;
                string value = field.GetRawConstantValue() as string;
                string title = display?.Name ?? L[name];
                string? desc = display?.Description;

                caps.Add(new RoleCapElement(value, title, desc));
            }

            DisplayAttribute? groupDisplay = model.GetCustomAttribute<DisplayAttribute>();

            groups.Add(new RoleCapGroup
            {
                Title = groupDisplay?.Name ?? L[model.Name],
                Description = groupDisplay?.Description,
                RoleCapElements = caps.ToArray()
            });

        }

        return groups;

    }

    List<FieldInfo> GetConstants(Type type)
    {
        FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public |
             BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();
    }

    EditRolesViewModelDto? _cachedEditRolesViewModelDto;

    public async Task<EditRolesViewModelDto> EditRolesViewModel()
    {
        throw new NotImplementedException();
        //if (_cachedEditRolesViewModelDto is not null) return _cachedEditRolesViewModelDto;

        //var ef = GetEFContext();

        //var roles = ef.Roles.AsNoTracking().ToList();

        //List<RoleClaimsDto> roleClaims = new List<RoleClaimsDto>();

        //var list = GetClaimsBlankList();
        //var listAsIds = list.SelectMany(s => s.RoleCapElements).Select(s => s.Id);

        //var allClaims = await ef.RoleClaims.AsNoTracking().ToListAsync();

        //var dict = allClaims.ToDictionary(s => $"{s.RoleId}+{s.ClaimType}+{s.ClaimValue}");

        //foreach (var role in roles)
        //{

        //    List<RoleCapGroupCheckable> groups = new List<RoleCapGroupCheckable>();

        //    foreach (var group in list)
        //    {

        //        var groupCheckable = new RoleCapGroupCheckable(group);

        //        foreach (var cap in groupCheckable.RoleCapElements)
        //        {
        //            //cap.Checked = true;
        //            cap.Checked = dict.ContainsKey($"{role.Id}++{cap.Id}");
        //        }

        //        groups.Add(groupCheckable);
        //    }

        //    roleClaims.Add(new RoleClaimsDto
        //    {
        //        Role = new RoleShortDto(role, L[role.Name]),
        //        Groups = groups.ToArray()

        //    });
        //}

        //return _cachedEditRolesViewModelDto = new EditRolesViewModelDto
        //{
        //    //Roles = roles,
        //    RoleClaims = roleClaims,
        //};
    }

    public async Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.ListAll(cancellationToken);

        throw new NotImplementedException();

        //var list = GetClaimsBlankList();
        //var listAsIds = list.SelectMany(s => s.RoleCapElements).Select(s => s.Id);

        //var allClaims = await _roleRepository.ListAllClaims(cancellationToken);

        //foreach (var role in roles)
        //{
        //    var roleClaim = dto.RoleClaims.FirstOrDefault(s => s.Role.Id == role.Id);

        //    if (roleClaim is null) continue;

        //    var existClaims = allClaims.Where(s => s.RoleId == role.Id && listAsIds.Contains(s.ClaimValue)).ToList();

        //    var setupClaims = roleClaim.Groups
        //                        .SelectMany(s => s.RoleCapElements)
        //                        .Where(s => s.Checked).ToList();

        //    var unsetupClaims = roleClaim.Groups
        //                        .SelectMany(s => s.RoleCapElements)
        //                        .Where(s => s.Checked == false).ToList();

        //    var forAddList = setupClaims.Select(s => s.Id).Except(existClaims.Select(e => e.ClaimValue)).ToList();
        //    var forRemoveList = existClaims.Select(s => s.ClaimValue).Intersect(unsetupClaims.Select(e => e.Id)).ToList();

        //    foreach (var cap in forAddList)
        //    {
        //        ef.RoleClaims.Add(new()
        //        {
        //            RoleId = role.Id,
        //            ClaimType = "",
        //            ClaimValue = cap
        //        });
        //    }

        //    var removeRange = existClaims.Where(s => s.RoleId == role.Id && forRemoveList.Contains(s.ClaimValue));

        //    ef.RoleClaims.RemoveRange(removeRange);
        //}

        //await ef.SaveChangesAsync();

        //_cachedEditRolesViewModelDto = null;

        //return new UserActionResult { Ok = true, Message = "Сохранено" };
    }

    public Task<bool> HasCap(string cap)
    {
        throw new NotImplementedException();
        //using var ef = GetEFContext();

        //if (!_user.Identity.IsAuthenticated) return false;

        //var userManager = _serviceProvider.GetService<UserManager<UserEntity>>();

        //var user = userManager.GetUserAsync(_user).Result;
        //var userRoles = userManager.GetRolesAsync(user).Result;

        ////var allClaims = ef.RoleClaims.AsNoTracking().Any(s => s.RoleId == );

        //var rolesCaps = await RolesCaps(userRoles);

        //return rolesCaps.Contains(cap);

    }

    async Task<IList<string>> RolesCaps(IList<string> roles)
    {
        var list = await EditRolesViewModel();

        //TODO: move to dictionary role.id+type+value
        return list.RoleClaims
                    .Where(s => roles.Contains(s.Role.Name))
                    .SelectMany(s => s.Groups)
                    .SelectMany(s => s.RoleCapElements)
                    .Where(s => s.Checked)
                    .Select(s => s.Id)
                    .Distinct()
                    .ToList();
    }
    #endregion

}
