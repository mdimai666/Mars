using Mars.Core.Exceptions;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.ViewModels;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.SiteSpace.Pages.RoleViews;

public partial class EditRolePage
{
    [Parameter] public Guid Id { get; set; }

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;

    StandardEditForm1<RoleEditModel> _editForm1 = default!;

    EditRolesViewModelDto? _matrix;
    Guid _loadedId;

    RoleClaimsDto? CurrentRole => _matrix?.RoleClaims.FirstOrDefault(r => r.Role.Id == Id);

    // Admin — суперпользователь: CapAuthorizationHandler обходит проверку прав по имени роли
    bool IsAdminRole => CurrentRole?.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

    RoleClaimsDto? SelectedRole
    {
        get => CurrentRole;
        set
        {
            if (value is null || value.Role.Id == Id) return;
            navigationManager.NavigateTo($"/dev/EditRole/{value.Role.Id}");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_matrix is null || _loadedId != Id || CurrentRole is null)
        {
            _matrix = await client.Role.EditRolesViewModel();
            _loadedId = Id;

            // при смене роли через селектор форма уже отрендерена — перезагружаем модель
            if (_editForm1 is not null)
            {
                await _editForm1.Load();
            }
        }
    }

    RoleEditModel? BuildModel()
    {
        var role = CurrentRole;
        if (role is null) return null;

        // Admin показываем со всеми правами независимо от claims в БД — handler всё равно обходит проверку
        if (IsAdminRole)
        {
            foreach (var cap in role.Groups.SelectMany(g => g.RoleCapElements))
            {
                cap.Checked = true;
            }
        }

        return new RoleEditModel
        {
            Id = role.Role.Id,
            Name = role.Role.Name,
            Title = role.Role.Title,
            Groups = role.Groups,
            IsSuperuser = IsAdminRole,
        };
    }

    async Task<RoleEditModel> SaveAction(RoleEditModel model, bool isNew)
    {
        var role = CurrentRole ?? throw new NotFoundException();

        var newName = model.Name.Trim();
        if (!model.IsSuperuser && !string.Equals(newName, role.Role.Name, StringComparison.Ordinal))
        {
            var updated = await client.Role.Update(new UpdateRoleRequest { Id = model.Id, Name = newName });
            role.Role.Name = updated.Name;
            model.Name = updated.Name;
        }

        // AfterSave мог перезагрузить матрицу — переносим отметки формы в актуальный снимок,
        // иначе сохранился бы старый массив без свежих правок
        role.Groups = model.Groups;

        var result = await client.Role.SaveRoleClaims(_matrix!);
        if (!result.Ok)
        {
            throw MarsValidationException.FromSingleError(nameof(RoleEditModel.Name), result.Message);
        }

        return model;
    }

    Task DeleteAction(RoleEditModel model) => client.Role.Delete(model.Id);

    void AfterDelete() => navigationManager.NavigateTo("/dev/Roles");

    async Task AfterSave(RoleEditModel model)
    {
        // свежий снимок: Title после rename и счётчики прав
        _matrix = await client.Role.EditRolesViewModel();
    }
}
