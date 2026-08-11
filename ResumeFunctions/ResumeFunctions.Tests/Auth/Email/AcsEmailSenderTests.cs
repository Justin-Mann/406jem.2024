using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth.Email;
using Xunit;

namespace ResumeFunctions.Tests.Auth.Email;

public class AcsEmailSenderTests
{
    // Well-formed but non-routable — EmailClient only parses the connection string locally
    // (no network call happens until SendAsync), so this is safe to construct in a unit test.
    private const string FakeConnectionString = "endpoint=https://fake.communication.azure.com/;accesskey=ZmFrZWtleQ==";

    private static IConfiguration BuildConfig(string? connectionString, string? senderAddress) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:AcsConnectionString"] = connectionString,
                ["Email:SenderAddress"] = senderAddress,
            })
            .Build();

    [Fact]
    public void Constructor_Throws_WhenConnectionStringMissing()
    {
        var config = BuildConfig(connectionString: null, senderAddress: "sender@406jem.com");

        Assert.Throws<InvalidOperationException>(() =>
            new AcsEmailSender(config, Substitute.For<ILogger<AcsEmailSender>>()));
    }

    [Fact]
    public void Constructor_Throws_WhenSenderAddressMissing()
    {
        var config = BuildConfig(connectionString: FakeConnectionString, senderAddress: null);

        Assert.Throws<InvalidOperationException>(() =>
            new AcsEmailSender(config, Substitute.For<ILogger<AcsEmailSender>>()));
    }

    [Fact]
    public void Constructor_Succeeds_WhenBothSettingsPresent()
    {
        var config = BuildConfig(connectionString: FakeConnectionString, senderAddress: "sender@406jem.com");

        var sender = new AcsEmailSender(config, Substitute.For<ILogger<AcsEmailSender>>());

        Assert.NotNull(sender);
    }
}
