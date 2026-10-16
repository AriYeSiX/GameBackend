using System;
using System.Collections.Generic;
using GameClient.Networking;

namespace GameClient.Core
{
    /// <summary>
    /// The single source of truth regarding the current match on the client. 
    /// Hub events can arrive at any time—including during scene transitions—
    /// so they are received by GameFlow and stored here, while scenes read the state and subscribe to changes.
    /// </summary>
    public sealed class MatchSession
    {
        private readonly Dictionary<Guid, long> _scores = new();

        public event Action Started;
        public event Action<Guid, long> ScoreChanged;
        public event Action Finished;
        public event Action RematchChanged;
        public event Action<string> Closed;

        public Guid MatchId { get; private set; }
        public bool IsActive => MatchId != Guid.Empty;
        public IReadOnlyList<MatchPlayerDto> Players { get; private set; } = Array.Empty<MatchPlayerDto>();

        public MatchStartedDto StartInfo { get; private set; }
        public bool IsStarted => StartInfo != null;

        /// <summary>Time.realtimeSinceStartupAsDouble at the moment MatchStarted is received.</summary>
        public double StartReceivedAt { get; private set; }

        public double CountdownEndsAt => StartReceivedAt + StartInfo.CountdownMs / 1000.0;
        public double RoundEndsAt => CountdownEndsAt + StartInfo.DurationMs / 1000.0;

        public MatchFinishedDto Results { get; private set; }
        public RematchStatusDto Rematch { get; private set; }
        public string ClosedReason { get; private set; }

        public long GetScore(Guid playerId) => _scores.TryGetValue(playerId, out var score) ? score : 0;

        public void Begin(MatchStartingDto match)
        {
            Reset();
            MatchId = match.MatchId;
            Players = match.Players;
        }

        public bool Apply(MatchStartedDto started, double receivedAt)
        {
            if (started.MatchId != MatchId)
                return false;

            StartInfo = started;
            StartReceivedAt = receivedAt;
            Players = started.Players; // someone may have failed to load and dropped out
            Started?.Invoke();
            return true;
        }

        public void Apply(ScoreChangedDto change)
        {
            if (change.MatchId != MatchId)
                return;

            // Events may arrive out of order; the score only grows
            if (change.Score <= GetScore(change.PlayerId))
                return;

            _scores[change.PlayerId] = change.Score;
            ScoreChanged?.Invoke(change.PlayerId, change.Score);
        }

        public void Apply(MatchFinishedDto finished)
        {
            if (finished.MatchId != MatchId)
                return;

            Results = finished;
            foreach (var result in finished.Results)
                _scores[result.PlayerId] = result.Score;

            Finished?.Invoke();
        }

        public void Apply(RematchStatusDto status)
        {
            if (status.MatchId != MatchId)
                return;

            Rematch = status;
            RematchChanged?.Invoke();
        }

        public void MarkClosed(string reason)
        {
            ClosedReason = reason;
            Closed?.Invoke(reason);
        }

        public void Reset()
        {
            MatchId = Guid.Empty;
            Players = Array.Empty<MatchPlayerDto>();
            StartInfo = null;
            StartReceivedAt = 0;
            Results = null;
            Rematch = null;
            ClosedReason = null;
            _scores.Clear();
        }
    }
}
