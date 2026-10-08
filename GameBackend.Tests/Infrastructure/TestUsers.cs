using GameBackend.Application.Auth;

namespace GameBackend.Tests.Infrastructure;

public static class TestUsers
{
    public static RegisterRequest New()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        return new RegisterRequest($"user_{id}", $"{id}@test.com", "Password123");
    }
}