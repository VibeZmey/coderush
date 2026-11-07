using Application.Common.Interfaces;
using Application.Common.Dto;
using Application.Features.Accounts.GetAccount;
using Application.Features.Accounts.Login;
using Application.Features.Accounts.Logout;
using Application.Features.Accounts.Register;
using Application.Features.Matches.CreateTrainingMatch;
using Application.Features.Matches.StartMatch;
using Application.Features.Matches.TrainingSession;
using Application.Features.Code.RunCode;
using Application.Features.Problems.CreateProblem;
using Application.Features.Problems.ManageProblems;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<IHandler<GetAccountQuery, AccountDto?>, GetAccountHandler>();
        services.AddScoped<IHandler<CreateTrainingMatchCommand, CreateTrainingMatchResult>, CreateTrainingMatchHandler>();
        services.AddScoped<IHandler<StartMatchCommand, StartMatchResult>, StartMatchHandler>();
        services.AddScoped<IHandler<GetTrainingMatchQuery, TrainingSessionResult>, GetTrainingMatchHandler>();
        services.AddScoped<IHandler<ListTrainingMatchesQuery, IReadOnlyList<TrainingMatchView>>, ListTrainingMatchesHandler>();
        services.AddScoped<IHandler<NextTrainingProblemCommand, TrainingSessionResult>, NextTrainingProblemHandler>();
        services.AddScoped<IHandler<LeaveTrainingMatchCommand, TrainingSessionResult>, LeaveTrainingMatchHandler>();
        services.AddScoped<IHandler<SaveTrainingDraftCommand, TrainingSessionResult>, SaveTrainingDraftHandler>();
        services.AddScoped<IHandler<RunCodeCommand, RunCodeResult>, RunCodeHandler>();
        services.AddScoped<IHandler<CreateProblemCommand, CreateProblemResult>, CreateProblemHandler>();
        services.AddScoped<ProblemManagementHandler>();
    
        return services;
    }
}
