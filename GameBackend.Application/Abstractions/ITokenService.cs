using GameBackend.Domain.Entities;

namespace GameBackend.Application.Abstractions;

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }
    string CreateAccessToken(Player player);
    string CreateRefreshToken();
    string HashToken(string token);
}