namespace Application.Features.Matches.StartMatch;

public class StartMatchCommand
{
    public string UserId { get; }
    public Guid MatchId { get; }

    public StartMatchCommand(string userId, Guid matchId)
    {
        UserId = userId;
        MatchId = matchId;
    }
}