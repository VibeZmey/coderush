using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class User : IdentityUser
{
    public DateTimeOffset? RegisteredAt { get; set; }
    public string? Bio { get; set; }
}
