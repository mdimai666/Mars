namespace Mars.Shared.ViewModels;

public class EditRolesViewModelDto
{
    //public List<Role> Roles { get; set; }
    //public List<Claim> Claims { get; set; }

    public IEnumerable<RoleClaimsDto> RoleClaims { get; set; }
}

public class RoleShortDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }

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
    public RoleShortDto Role { get; set; }
    public RoleCapGroupCheckable[] Groups { get; set; }
}
