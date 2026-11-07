using Application.Common.Interfaces;

namespace Application.Features.Accounts.Login;

public sealed class LoginHandler(IIdentityGateway identity)
{
    public async Task<LoginResult> HandleAsync(LoginCommand command)
    {
        var succeeded = await identity.SignInAsync(
            command.Email.Trim(), command.Password, command.RememberMe);

        return succeeded ? LoginResult.Succeeded : LoginResult.InvalidCredentials;
    }
}
