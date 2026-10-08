using GameBackend.Tests.Infrastructure;

namespace GameBackend.Tests;

[SetUpFixture]
public class TestEnvironment
{
    public static ApiFactory Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        Factory = new ApiFactory();
        await Factory.StartAsync();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await Factory.DisposeAsync();
    }
}