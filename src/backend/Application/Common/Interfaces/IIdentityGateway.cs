using Application.Common.Dto;
using Application.Features.Accounts.Register;

namespace Application.Common.Interfaces;

// The implementation owns Identity persistence and the authentication cookie.
public interface IIdentityGateway
{
    Task<RegistrationResult> CreateAccountAsync(string email, string password, string userName);
    Task<bool> SignInAsync(string email, string password, bool rememberMe);
    Task SignOutAsync();
    Task<AccountDto?> FindAccountAsync(string userId);
}
