namespace OrderService.Application.Accounts;

public sealed class DuplicateUsernameException : Exception
{
    public DuplicateUsernameException() : base("Username is already taken.")
    {
    }
}
