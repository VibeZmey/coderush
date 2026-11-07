namespace Application.Features.Code.RunCode;

public sealed record RunCodeCommand(
    string UserId,
    Guid MatchProblemId,
    int ProgrammingLanguageId,
    string SourceCode,
    string StandardInput);
