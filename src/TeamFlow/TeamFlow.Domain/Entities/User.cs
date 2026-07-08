using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public Role Role { get; private set; }

    private User() 
    {
    }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        Role role,
        DateTimeOffset now)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            CreatedAt = now
        };
    }

    public void UpdateProfile(string firstName, string lastName, DateTimeOffset updatedAt)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = updatedAt;
    }
}
