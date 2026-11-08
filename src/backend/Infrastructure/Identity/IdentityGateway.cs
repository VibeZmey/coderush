using Application.Common.Interfaces;
using Application.Common.Dto;
using Application.Features.Accounts.Register;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Identity;

public sealed class IdentityGateway(
    UserManager<User> users, SignInManager<User> signIn) : IIdentityGateway
{
    public async Task<RegistrationResult> CreateAccountAsync(
        string email, string password, string userName)
    {
        var user = new User { Email = email, UserName = userName, RegisteredAt = DateTimeOffset.UtcNow };
        IdentityResult result;
        try
        {
            result = await users.CreateAsync(user, password);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "EmailIndex" or "UserNameIndex" } conflict)
        {
            var duplicate = conflict.ConstraintName == "EmailIndex"
                ? users.ErrorDescriber.DuplicateEmail(email)
                : users.ErrorDescriber.DuplicateUserName(userName);
            result = IdentityResult.Failed(duplicate);
        }
        if (!result.Succeeded)
            return RegistrationResult.Failure(result.Errors
                .Select(error => new RegistrationError(error.Code, error.Description)).ToArray());

        return RegistrationResult.Success(ToDto(user));
    }

    public async Task<bool> SignInAsync(string email, string password, bool rememberMe)
    {
        var user = email.Contains('@') ? await users.FindByEmailAsync(email) : await users.FindByNameAsync(email);
        if (user is null)
            return false;

        var result = await signIn.PasswordSignInAsync(
            user, password, isPersistent: rememberMe, lockoutOnFailure: true);
        // This password-only flow does not complete a two-factor challenge.
        return result.Succeeded;
    }

    public Task SignOutAsync() => signIn.SignOutAsync();

    public async Task<AccountDto?> FindAccountAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        return user is null ? null : ToDto(user);
    }

    private static AccountDto ToDto(User user) => new(user.Id, user.Email, user.UserName);
}
