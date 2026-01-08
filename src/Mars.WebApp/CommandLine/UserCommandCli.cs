using System.CommandLine;
using Mars.Core.Utils;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Dto.Roles;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Dto.Users.Passwords;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Utils;
using Mars.Shared.Common;

namespace Mars.CommandLine;

public class UserCommandCli : CommandCli
{
    public UserCommandCli(CommandLineApi cli) : base(cli)
    {
        var argumentUsername = new Argument<string>("username");
        var optionUsername = new Option<string>("--username") { Required = true };

        var optionFilter = new Option<string>("--filter", "-f") { Description = "Reg ex filter result" };

        var optionEmail = new Option<string>("--email", "-m") { Description = "Email" };
        var optionPassword = new Option<string>("--password", "-p") { Description = "Password" };
        var optionRole = new Option<string>("--role") { Description = "Role" };

        var optionFirstName = new Option<string>("--firstName") { Description = "First name" };
        var optionLastName = new Option<string>("--lastName") { Description = "Last name" };
        var optionUserTypeName = new Option<string>("--userTypeName") { Description = "User type name" };

        var showIdArgument = new Option<bool>("-id") { Description = "Show Id Column" };

        var userCommand = new Command("user", "users manage subcommand");

        //add
        var userAddCommand = new Command("add", "add user command")
        {
            optionUsername,optionEmail,optionPassword,optionRole,optionFirstName,optionLastName
        };
        userAddCommand.SetAction((p, ct) =>
        {
            return UserAddCommand(p.GetRequiredValue(optionUsername),
                            p.GetValue(optionEmail),
                            p.GetValue(optionPassword),
                            p.GetValue(optionRole),
                            p.GetValue(optionFirstName),
                            p.GetValue(optionLastName),
                            p.GetValue(optionUserTypeName),
                            ct);
        });
        userCommand.Subcommands.Add(userAddCommand);

        //list
        var userListCommand = new Command("list", "list users") { optionFilter, showIdArgument };
        userListCommand.SetAction((p, ct) => UserListCommand(p.GetValue(optionFilter), p.GetValue(showIdArgument), ct));
        userCommand.Subcommands.Add(userListCommand);

        //delete
        var userDeleteCommand = new Command("delete", "delete user") { argumentUsername };
        userDeleteCommand.SetAction((p, ct) => UserDeleteCommand(p.GetRequiredValue(argumentUsername), ct));
        userCommand.Subcommands.Add(userDeleteCommand);

        //password
        var passwordArgument = new Argument<string>("password");
        var userSetPassword = new Command("setpassword") { argumentUsername, passwordArgument };
        userSetPassword.SetAction((p, ct) => UserSetPassword(p.GetRequiredValue(argumentUsername), p.GetRequiredValue(passwordArgument), ct));
        userCommand.Subcommands.Add(userSetPassword);

        var userSetNewPassword = new Command("setnewpassword") { argumentUsername };
        userSetNewPassword.SetAction((p, ct) => UserSetNewPassword(p.GetRequiredValue(argumentUsername), ct));
        userCommand.Subcommands.Add(userSetNewPassword);

        cli.AddCommand(userCommand);
    }

    public async Task UserAddCommand(string username, string? email, string? password, string? role, string? firstName, string? lastName, string? userType,
                                    CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        RoleSummary? roleEntity = null;

        if (role is not null)
        {
            var roles = await roleRepo.ListAll(CancellationToken.None);
            roleEntity = roles.FirstOrDefault(s => s.Name.Equals(role, StringComparison.OrdinalIgnoreCase));
            if (roleEntity is null)
            {
                Console.WriteLine($"Role '{role}' not found");
                Console.WriteLine($"    avail roles is: ");
                var roleCommand = cli.GetCommand<RoleCommandCli>();
                await roleCommand.RoleListCommand(null, cancellationToken);
                throw new InvalidOperationException();
            }
        }

        if (password is not null && password.Length < 3)
            throw new InvalidOperationException("password too short (min: 3)");
        string setPassword = password ?? Password.Generate(8, 2);

        string[] setRole = roleEntity is null ? Array.Empty<string>() : [roleEntity.Name];

        try
        {
            var createdId = await userRepo.Create(new CreateUserQuery
            {
                Email = email,
                Password = setPassword,
                UserName = username,
                FirstName = firstName ?? username,
                LastName = lastName ?? "",
                Roles = setRole,
                AvatarUrl = null,

                Type = userType ?? UserTypeEntity.DefaultTypeName,
                MetaValues = [],
            }, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            if (password is null)
                Console.WriteLine($"Password is: {setPassword}");

            Console.WriteLine("User creared! username: " + username);
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }

    public async Task UserListCommand(string? filter, bool showIdColumn, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var users = await userRepo.ListDetail(new ListUserQuery { Search = filter }, cancellationToken);
        var usersArray = users.Items.Select(s => new string[] {
            s.Id.ToString(), s.UserName, s.Email??string.Empty, s.FirstName, s.LastName, string.Join(',', s.Roles)
        }).ToList();

        if (!showIdColumn)
        {
            usersArray = usersArray.Select(s => s.Skip(1).ToArray()).ToList();
        }

        var table = new ConsoleTable([
            showIdColumn?
            ["Id", "Username", "Email", "FirstName", "LastName", "Roles"]
            :
            ["Username", "Email", "FirstName", "LastName", "Roles"],
            ..usersArray
        ]);

        Console.WriteLine(table);
    }

    public async Task UserDeleteCommand(string name, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetDetailByUserName(name, cancellationToken);

        if (user is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"User '{name}' not found");
            Console.ResetColor();
            return;
        }
        var userDto = await userRepo.GetDetail(user.Id, CancellationToken.None);
        var roles = string.Join(',', userDto.Roles);

        if (Confirm($"Do you really want delete user - {user} (email:{user.Email}, role: {roles}, id: {user.Id})"))
        {
            await userRepo.Delete(user.Id, CancellationToken.None);
            OutResult(UserActionResult.Success("Успешно удалено"));
        }
    }

    public async Task UserSetPassword(string username, string password, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await userRepo.SetPassword(new SetUserPasswordQuery() { NewPassword = password, Username = username }, cancellationToken);

        OutResult(result);
    }

    public async Task UserSetNewPassword(string username, CancellationToken cancellationToken)
    {
        var password = Password.Generate(8, 2);
        await UserSetPassword(username, password, cancellationToken);
        Console.WriteLine($"New password: {password}");
    }

}
