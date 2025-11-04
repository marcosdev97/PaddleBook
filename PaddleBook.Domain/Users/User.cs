namespace PaddleBook.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User(string email, string? fullName = null)
    {
        Id = Guid.NewGuid();
        Email = email;
        FullName = fullName;
    }
}
