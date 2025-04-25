using Hangfire;
using Hangfire.Server;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Hosting;
using Hangfire.Idempotent;

namespace TestHangfireIdempotent;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void UseExtension()
    {
        GlobalConfiguration.Configuration
            .UseMemoryStorage()
            .UseIdempotent();

        Assert.Pass();
    }
}
