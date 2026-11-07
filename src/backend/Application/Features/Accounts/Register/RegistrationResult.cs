using Application.Common.Dto;

namespace Application.Features.Accounts.Register;

public sealed record RegistrationResult
{
    public AccountDto? Account { get; }
    public IReadOnlyList<RegistrationError> Errors { get; }
    public bool Succeeded => Account is not null;

    private RegistrationResult(AccountDto? account, IReadOnlyList<RegistrationError> errors)
    {
        Account = account;
        Errors = errors;
    }

    public static RegistrationResult Success(AccountDto account) =>
        new(account, []);
    public static RegistrationResult Failure(IReadOnlyList<RegistrationError> errors) =>
        new(null, errors);
}

public sealed record RegistrationError(string Code, string Description);
