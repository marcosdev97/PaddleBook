using Microsoft.AspNetCore.Identity;

namespace PaddleBook.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
}
