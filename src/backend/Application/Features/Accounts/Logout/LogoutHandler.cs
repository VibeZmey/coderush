using Application.Common.Interfaces;

namespace Application.Features.Accounts.Logout;

public sealed class LogoutHandler(IIdentityGateway identity)
{
    public Task HandleAsync() => identity.SignOutAsync();
}
