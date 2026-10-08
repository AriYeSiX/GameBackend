using System.Net;
using System.Net.Http.Json;
using GameBackend.Application.Saves;

namespace GameBackend.Tests.Saves;

[TestFixture]
public class SavesTests
{
    [Test]
    public async Task Put_ThenGet_ReturnsSameData()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var put = await client.PutAsJsonAsync("/api/saves/1",
            new { data = new { level = 5, gold = 120 }, expectedVersion = 0 });
        put.EnsureSuccessStatusCode();

        var save = await client.GetFromJsonAsync<SaveSlotResponse>("/api/saves/1");

        Assert.That(save, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(save!.Version, Is.EqualTo(1));
            Assert.That(save.Data.GetProperty("level").GetInt32(), Is.EqualTo(5));
        });
    }

    [Test]
    public async Task Put_WithStaleVersion_Returns409()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
        var body = new { data = new { level = 1 }, expectedVersion = 0 };

        (await client.PutAsJsonAsync("/api/saves/1", body)).EnsureSuccessStatusCode();
        var stale = await client.PutAsJsonAsync("/api/saves/1", body);

        Assert.That(stale.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Get_EmptySlot_Returns404()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/saves/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Put_SlotOutOfRange_Returns400()
    {
        var (client, _) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/saves/99",
            new { data = new { level = 1 }, expectedVersion = 0 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}