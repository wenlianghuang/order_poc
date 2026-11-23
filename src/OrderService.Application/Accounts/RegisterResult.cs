namespace OrderService.Application.Accounts;

public abstract record RegisterResult
{
    public sealed record Success(Guid Id, string Username) : RegisterResult;

    public sealed record InvalidInput(IDictionary<string, string[]> Errors) : RegisterResult;

    public sealed record UsernameTaken : RegisterResult;
}
