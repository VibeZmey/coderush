namespace Application.Features.Accounts.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe);
