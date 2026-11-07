using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required]
    public required string UserName { get; init; }
}
