using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Application.Accounts;

public sealed class AccountService
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;

    public AccountService(IUserRepository users, IPasswordHasher passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
            return new RegisterResult.InvalidInput(errors);

        var username = command.Username!.Trim();
        if (await _users.FindByUsernameAsync(username, cancellationToken) is not null)
            return new RegisterResult.UsernameTaken();

        var user = User.Create(username, _passwords.Hash(command.Password!));
        try
        {
            await _users.AddAsync(user, cancellationToken);
        }
        catch (DuplicateUsernameException)
        {
            return new RegisterResult.UsernameTaken();
        }

        return new RegisterResult.Success(user.Id, user.Username);
    }

    public async Task<Guid?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _users.FindByUsernameAsync(username, cancellationToken);
        if (user is null || !_passwords.Verify(password, user.PasswordHash))
            return null;

        return user.Id;
    }

    private static Dictionary<string, string[]> Validate(RegisterCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Username))
            errors["username"] = ["Username is required."];
        else if (command.Username.Trim().Length > User.UsernameMaxLength)
            errors["username"] = [$"Username must be {User.UsernameMaxLength} characters or fewer."];

        if (string.IsNullOrWhiteSpace(command.Password))
            errors["password"] = ["Password is required."];
        else if (command.Password.Length < PasswordMinLength)
            errors["password"] = [$"Password must be at least {PasswordMinLength} characters."];
        else if (command.Password.Length > PasswordMaxLength)
            errors["password"] = [$"Password must be {PasswordMaxLength} characters or fewer."];

        return errors;
    }
}
