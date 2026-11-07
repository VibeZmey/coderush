namespace Application.Features.Accounts.Register;

public sealed record RegisterAccountCommand(
    string Email,
    string Password,
    string UserName);
