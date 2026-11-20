using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Contracts.Common;
using Mars.Contracts.Resources;
using Mars.Core.Exceptions;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.Extensions.Localization;

namespace Mars.Identity.Host.Services;

public class RoleService : IRoleService
{
    readonly IStringLocalizer<AppRes> L;
    private readonly IEventManager _eventManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IRequestContext _requestContext;
    private readonly IRoleCapsCache _roleCapsCache;

    public RoleService(IRoleRepository roleRepository, IStringLocalizer<AppRes> stringLocalizer, IEventManager eventManager,
        IRequestContext requestContext, IRoleCapsCache roleCapsCache)
    {
        L = stringLocalizer;
        _eventManager = eventManager;
        _roleRepository = roleRepository;
        _requestContext = requestContext;
        _roleCapsCache = roleCapsCache;
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

        var payload = new ManagerEventPayload(_eventManager.Defaults.RoleAdd(), created!);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        _roleCapsCache.Invalidate();

        return created!;
    }

    public async Task<RoleDetail> Update(UpdateRoleQuery query, CancellationToken cancellationToken)
    {
        await _roleRepository.Update(query, cancellationToken);
        var updated = (await Get(query.Id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.RoleUpdate(), updated);
        _eventManager.TriggerEvent(payload);

        // rename роли меняет ключ снимка кэша
        _roleCapsCache.Invalidate();

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _roleRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.RoleDelete(), role);
        _eventManager.TriggerEvent(payload);

        _roleCapsCache.Invalidate();

        return UserActionResult.Success();
    }

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

        List<RoleCapGroup> groups = [];

        foreach (Type model in internals)
        {
            var constants = GetConstants(model);

            List<RoleCapElement> caps = [];

            foreach (var field in constants)
            {
                var display = field.GetCustomAttribute<DisplayAttribute>();

                string name = field.Name;
                string value = (field.GetRawConstantValue() as string)!;
                // ключ "{группа}.{право}" различает одноимённые права разных групп
                // (Manage/Add/...); fallback на короткое имя — для прав без своего ключа
                var localized = L[$"{model.Name}.{name}"];
                string title = display?.Name ?? (localized.ResourceNotFound ? L[name].Value : localized.Value);
                string desc = display?.Description ?? "";

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

    public async Task<EditRolesViewModelDto> EditRolesViewModel()
    {
        var roles = await _roleRepository.ListAll(CancellationToken.None);
        var allClaims = await _roleRepository.ListAllClaims(CancellationToken.None);

        var granted = allClaims
            .Where(s => s.ClaimType == RoleCaps.ClaimType)
            .Select(s => (s.RoleId, s.ClaimValue))
            .ToHashSet();

        var groups = GetClaimsBlankList();

        var roleClaims = roles.Select(role => new RoleClaimsDto
        {
            Role = new RoleShortDto
            {
                Id = role.Id,
                Name = role.Name,
                Title = L[role.Name],
            },
            Groups = groups.Select(group =>
            {
                var checkable = new RoleCapGroupCheckable(group);
                foreach (var cap in checkable.RoleCapElements)
                {
                    cap.Checked = granted.Contains((role.Id, cap.Id));
                }
                return checkable;
            }).ToArray(),
        });

        return new EditRolesViewModelDto
        {
            RoleClaims = roleClaims,
        };
    }

    public async Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto, CancellationToken cancellationToken)
    {
        var knownCaps = GetClaimsBlankList()
            .SelectMany(s => s.RoleCapElements)
            .Select(s => s.Id)
            .ToHashSet();

        var roleIds = (await _roleRepository.ListAll(cancellationToken))
            .Select(s => s.Id)
            .ToHashSet();

        foreach (var roleClaim in dto.RoleClaims)
        {
            // роли не из БД игнорируем — иначе получились бы claims-сироты
            if (!roleIds.Contains(roleClaim.Role.Id)) continue;

            var caps = roleClaim.Groups
                .SelectMany(s => s.RoleCapElements)
                .Where(s => s.Checked && knownCaps.Contains(s.Id))
                .Select(s => s.Id)
                .ToHashSet();

            await _roleRepository.SetRoleClaims(roleClaim.Role.Id, caps, cancellationToken);
        }

        _roleCapsCache.Invalidate();

        return UserActionResult.Success();
    }

    public async Task<bool> HasCap(string cap)
    {
        var roles = _requestContext.Roles;
        if (roles is null || roles.Count == 0) return false;

        var roleCaps = await _roleCapsCache.GetRoleCaps();

        return roles.Any(role => roleCaps.TryGetValue(role, out var caps) && caps.Contains(cap));
    }
}
