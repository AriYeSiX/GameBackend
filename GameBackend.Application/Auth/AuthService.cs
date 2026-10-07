using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Players.AnyAsync(p => p.Email == email || p.Username == request.Username, ct))
            throw new ConflictException("Игрок с таким email или ником уже существует");

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = Now
        };

        db.Players.Add(player);
        var response = IssueTokens(player);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var player = await db.Players.FirstOrDefaultAsync(p => p.Email == email, ct);

        if (player is null || !passwordHasher.Verify(player.PasswordHash, request.Password))
            throw new UnauthorizedException("Неверный email или пароль");

        var response = IssueTokens(player);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive(Now))
            throw new UnauthorizedException("Недействительный refresh-токен");

        stored.RevokedAt = Now;
        var response = IssueTokens(stored.Player);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = Now;
            await db.SaveChangesAsync(ct);
        }
    }

    private AuthResponse IssueTokens(Player player)
    {
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = tokenService.HashToken(refreshToken),
            CreatedAt = Now,
            ExpiresAt = Now + tokenService.RefreshTokenLifetime
        });

        return new AuthResponse(tokenService.CreateAccessToken(player), refreshToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}