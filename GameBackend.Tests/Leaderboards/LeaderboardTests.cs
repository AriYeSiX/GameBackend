using System.Net;
using System.Net.Http.Json;
using GameBackend.Application.Leaderboards;
using GameBackend.Application.Players;

namespace GameBackend.Tests.Leaderboards;

[TestFixture]
public class LeaderboardTests
{
    private const string Board = "/api/leaderboards/classic";

    [Test]
    public async Task Submit_HigherScores_AreRankedInOrder()
    {
        var baseScore = Random.Shared.NextInt64(1_000_000, 900_000_000_000);
        var ranks = new List<long>();

        foreach (var offset in new[] { 3, 2, 1 })
        {
            var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
            var response = await client.PostAsJsonAsync($"{Board}/scores",
                new SubmitScoreRequest(baseScore + offset));
            var result = await response.Content.ReadFromJsonAsync<SubmitScoreResponse>();
            ranks.Add(result!.Rank);
        }

        Assert.That(ranks, Is.Ordered.Ascending);
        Assert.That(ranks, Is.Unique);
    }

    [Test]
    public async Task Submit_LowerScore_KeepsBest()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync($"{Board}/scores", new SubmitScoreRequest(500));
        var response = await client.PostAsJsonAsync($"{Board}/scores", new SubmitScoreRequest(100));
        var result = await response.Content.ReadFromJsonAsync<SubmitScoreResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(result!.BestScore, Is.EqualTo(500));
            Assert.That(result.IsNewBest, Is.False);
        });
    }

    [Test]
    public async Task Top_ContainsPlayerWithHighestScore()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
        var me = await client.GetFromJsonAsync<PlayerResponse>("/api/players/me");

        await client.PostAsJsonAsync($"{Board}/scores", new SubmitScoreRequest(1_000_000_000_000));
        var top = await client.GetFromJsonAsync<List<LeaderboardEntry>>($"{Board}/top?count=10");

        Assert.That(top!.Select(e => e.PlayerId), Does.Contain(me!.Id));
    }

    [Test]
    public async Task Submit_UnknownLeaderboard_Returns404()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/leaderboards/unknown/scores",
            new SubmitScoreRequest(100));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}