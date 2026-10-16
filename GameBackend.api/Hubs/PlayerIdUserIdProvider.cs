using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GameBackend.Api.Hubs;

/// <summary>
/// Tells SignalR that the "user" of a connection is the player ID from the sub claim.
/// This lets match events be sent via Clients.Users(playerIds)
/// without knowing every player's connectionId.
/// </summary>
public class PlayerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
