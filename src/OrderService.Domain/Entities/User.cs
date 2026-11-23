using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

public sealed class User
{
    public const int UsernameMaxLength = 64;
    public const int PasswordHashMaxLength = 200;

    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string NormalizedUsername { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private User()
    {
    }

    public static User Create(string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username is required.");

        var trimmed = username.Trim();
        if (trimmed.Length > UsernameMaxLength)
            throw new DomainException($"Username must be {UsernameMaxLength} characters or fewer.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        if (passwordHash.Length > PasswordHashMaxLength)
            throw new DomainException($"Password hash must be {PasswordHashMaxLength} characters or fewer.");

        return new User
        {
            Id = Guid.NewGuid(),
            Username = trimmed,
            NormalizedUsername = trimmed.ToUpperInvariant(),
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
