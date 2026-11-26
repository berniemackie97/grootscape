using System.Text.Json;
using Xunit;

namespace Grootscape.Protocol.Tests;

/// <summary>Verifies the codec against the shared fixtures in <c>protocol/fixtures</c>.</summary>
public sealed class FixtureTests
{
    private static readonly JsonElement Fixtures = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "v1.json"))).RootElement;

    public static TheoryData<string> ClientMessages => Names("messages", "clientToServer");

    public static TheoryData<string> ServerMessages => Names("messages", "serverToClient");

    public static TheoryData<string> InvalidClientMessages => Names("invalid", "clientToServer");

    [Fact]
    public void FixturesMatchCodecVersion() =>
        Assert.Equal(WireProtocol.Version, Fixtures.GetProperty("protocolVersion").GetUInt16());

    [Theory]
    [MemberData(nameof(ClientMessages))]
    public void DecodesClientMessage(string name)
    {
        JsonElement fixture = Case("messages", name);
        JsonElement message = fixture.GetProperty("message");
        Assert.Equal("clientHello", message.GetProperty("type").GetString());
        Assert.Equal(WireProtocol.Version, message.GetProperty("protocolVersion").GetUInt16());

        Assert.True(ClientHello.TryDecode(Bytes(fixture), out ClientHello hello, out DecodeError error));
        Assert.Equal(DecodeError.None, error);
        Assert.Equal(ParsePlatform(message.GetProperty("platform").GetString()), hello.Platform);
    }

    [Theory]
    [MemberData(nameof(ServerMessages))]
    public void EncodesServerMessage(string name)
    {
        JsonElement fixture = Case("messages", name);
        JsonElement message = fixture.GetProperty("message");
        byte[] encoded = message.GetProperty("type").GetString() switch
        {
            "serverWelcome" => new ServerWelcome(message.GetProperty("tickDurationMs").GetUInt16()).Encode(),
            "serverReject" => new ServerReject(ParseReason(message.GetProperty("reason").GetString())).Encode(),
            string other => throw new InvalidOperationException($"Unknown server message {other}"),
            null => throw new InvalidOperationException("Missing message type"),
        };

        Assert.Equal(Bytes(fixture), encoded);
    }

    [Theory]
    [MemberData(nameof(InvalidClientMessages))]
    public void RejectsInvalidClientMessage(string name)
    {
        JsonElement fixture = Case("invalid", name);

        Assert.False(ClientHello.TryDecode(Bytes(fixture), out _, out DecodeError error));
        Assert.Equal(fixture.GetProperty("error").GetString(), JsonNamingPolicy.CamelCase.ConvertName(error.ToString()));
    }

    private static TheoryData<string> Names(string list, string direction)
    {
        TheoryData<string> names = [];
        foreach (JsonElement fixture in Fixtures.GetProperty(list).EnumerateArray())
        {
            if (fixture.GetProperty("direction").GetString() == direction)
            {
                names.Add(fixture.GetProperty("name").GetString()!);
            }
        }

        return names;
    }

    private static JsonElement Case(string list, string name) =>
        Fixtures.GetProperty(list).EnumerateArray().Single(fixture => fixture.GetProperty("name").GetString() == name);

    private static byte[] Bytes(JsonElement fixture) => Convert.FromHexString(fixture.GetProperty("hex").GetString()!);

    private static Platform ParsePlatform(string? name) => name switch
    {
        "windows" => Platform.Windows,
        "linux" => Platform.Linux,
        "macos" => Platform.MacOS,
        "android" => Platform.Android,
        "web" => Platform.Web,
        _ => throw new InvalidOperationException($"Unknown platform {name}"),
    };

    private static RejectReason ParseReason(string? name) => name switch
    {
        "unsupportedVersion" => RejectReason.UnsupportedVersion,
        "malformed" => RejectReason.Malformed,
        _ => throw new InvalidOperationException($"Unknown reject reason {name}"),
    };
}
