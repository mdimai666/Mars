namespace Mars.Shared.ViewModels;

public class EditRolesViewModelDto
{
    //public List<Role> Roles { get; set; }
    //public List<Claim> Claims { get; set; }

    public IEnumerable<RoleClaimsDto> RoleClaims { get; set; } = [];
}

public class RoleShortDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public RoleShortDto()
    {

    }
    public RoleShortDto(RoleShortDto role, string title)
    {
        Id = role.Id;
        Name = role.Name;
        Title = title;
    }
}

public class RoleClaimsDto
{
    public required RoleShortDto Role { get; set; }
    public required RoleCapGroupCheckable[] Groups { get; set; }
}
