using System.Net;
using System.Net.Http.Json;
using GameBackend.Application.Auth;
using GameBackend.Application.Players;
using GameBackend.Tests.Infrastructure;

namespace GameBackend.Tests.Auth;

[TestFixture]
public class AuthTests
{
    [Test]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
        var user = TestUsers.New();
        (await client.PostAsJsonAsync("/api/auth/register", user)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            user with { Username = user.Username + "x" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = TestEnvironment.Factory.CreateClient();
        var user = TestUsers.New();
        (await client.PostAsJsonAsync("/api/auth/register", user)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(user.Email, "WrongPassword"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await TestEnvironment.Factory.CreateClient().GetAsync("/api/players/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_WithToken_ReturnsPlayer()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var player = await client.GetFromJsonAsync<PlayerResponse>("/api/players/me");

        Assert.That(player, Is.Not.Null);
        Assert.That(player!.Username, Does.StartWith("user_"));
    }

    [Test]
    public async Task Refresh_RotatesToken_OldTokenIsRejected()
    {
        var (client, tokens) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens.RefreshToken));
        var second = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens.RefreshToken));

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }
}