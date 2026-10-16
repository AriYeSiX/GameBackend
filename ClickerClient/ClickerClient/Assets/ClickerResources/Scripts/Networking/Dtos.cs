using System;
using System.Collections.Generic;

namespace GameClient.Networking
{
    // ---------- Auth and profile ----------

    public class RegisterRequest { public string Username { get; set; } public string Email { get; set; } public string Password { get; set; } }
    public class LoginRequest { public string Email { get; set; } public string Password { get; set; } }
    public class RefreshRequest { public string RefreshToken { get; set; } }
    public class AuthResponse { public string AccessToken { get; set; } public string RefreshToken { get; set; } }
    public class PlayerProfileDto { public Guid Id { get; set; } public string Username { get; set; } public string Email { get; set; } }

    // ---------- Leaderboard ----------

    public class LeaderboardEntry { public long Rank { get; set; } public Guid PlayerId { get; set; } public string Username { get; set; } public long Score { get; set; } }
    public class PlayerRankResponse { public long Rank { get; set; } public long Score { get; set; } }

    // ---------- Lobby ----------

    public class LobbyMemberDto { public Guid PlayerId { get; set; } public string Username { get; set; } public bool IsReady { get; set; } }

    public class LobbyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid HostId { get; set; }
        public int MaxPlayers { get; set; }
        public string State { get; set; }
        public List<LobbyMemberDto> Members { get; set; } = new List<LobbyMemberDto>();
    }

    // ---------- Match ----------

    public class MatchPlayerDto { public Guid PlayerId { get; set; } public string Username { get; set; } }

    public class MatchStartingDto { public Guid MatchId { get; set; } public List<MatchPlayerDto> Players { get; set; } = new List<MatchPlayerDto>(); }

    public class MatchStartedDto
    {
        public Guid MatchId { get; set; }
        public int CountdownMs { get; set; }
        public int DurationMs { get; set; }
        public List<MatchPlayerDto> Players { get; set; } = new List<MatchPlayerDto>();
    }

    public class ScoreChangedDto { public Guid MatchId { get; set; } public Guid PlayerId { get; set; } public long Score { get; set; } }

    public class PlayerResultDto
    {
        public Guid PlayerId { get; set; }
        public string Username { get; set; }
        public long Score { get; set; }
        public bool IsWinner { get; set; }
        public long BestScore { get; set; }
        public long Rank { get; set; }
        public bool IsNewBest { get; set; }
    }

    public class MatchFinishedDto { public Guid MatchId { get; set; } public List<PlayerResultDto> Results { get; set; } = new List<PlayerResultDto>(); }

    public class RematchStatusDto
    {
        public Guid MatchId { get; set; }
        public List<Guid> ReadyPlayerIds { get; set; } = new List<Guid>();
        public List<Guid> ActivePlayerIds { get; set; } = new List<Guid>();
    }

    public class MatchClosedDto { public Guid MatchId { get; set; } public string Reason { get; set; } }

    // ---------- Errors ----------

    public class ProblemDetailsDto { public string Title { get; set; } public int? Status { get; set; } }
}
