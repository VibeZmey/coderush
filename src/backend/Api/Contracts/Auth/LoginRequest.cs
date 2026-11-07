using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}
