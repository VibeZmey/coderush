using Application.Common.Interfaces;
using Application.Common.Dto;

namespace Application.Features.Accounts.GetAccount;

public sealed class GetAccountHandler(IIdentityGateway identity) : IHandler<GetAccountQuery, AccountDto?>
{
    public Task<AccountDto?> HandleAsync(GetAccountQuery query) =>
        identity.FindAccountAsync(query.UserId);
}
