using GameBackend.Domain.Matches;

namespace GameBackend.Infrastructure.Matches;

internal record MatchParticipantDocument(
    Guid PlayerId, string Username, bool IsLoaded, bool HasLeft, bool WantsRematch, long Score);

internal record MatchDocument(
    Guid Id, MatchPhase Phase, MatchTimings Timings, DateTime CreatedAt, DateTime Deadline,
    DateTime? StartsAt, DateTime? EndsAt, List<MatchParticipantDocument> Participants)
{
    public static MatchDocument From(Match match) => new(
        match.Id, match.Phase, match.Timings, match.CreatedAt, match.Deadline, match.StartsAt, match.EndsAt,
        match.Participants
            .Select(p => new MatchParticipantDocument(p.PlayerId, p.Username, p.IsLoaded, p.HasLeft, p.WantsRematch, p.Score))
            .ToList());

    public Match ToDomain() => Match.Restore(
        Id, Phase, Timings, CreatedAt, Deadline, StartsAt, EndsAt,
        Participants.Select(p => new MatchParticipant(p.PlayerId, p.Username, p.IsLoaded, p.HasLeft, p.WantsRematch, p.Score)));
}
