using GameBackend.Application.Abstractions;
using GameBackend.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GameBackend.Infrastructure.Auth;

public class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Player> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}