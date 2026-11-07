using Application.Common.Interfaces;

namespace Application.Features.Accounts.Register;

public sealed class RegisterAccountHandler(IIdentityGateway identity)
{
    public Task<RegistrationResult> HandleAsync(RegisterAccountCommand command)
    {
        // Identity checks email format, username uniqueness and password policy.
        return identity.CreateAccountAsync(
            command.Email.Trim(), command.Password, command.UserName.Trim());
    }
}
