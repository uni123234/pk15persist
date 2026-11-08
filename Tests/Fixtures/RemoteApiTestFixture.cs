using System.Net;
using System.Text;
using System.Text.Json;
using Project.Infra.Remote;
using Project.Infra.Remote.Models;

namespace Tests.Fixtures;

/// <summary>
/// [Testing Pattern: xUnit Test Fixture (IClassFixture<T>)]
/// Provides shared context, mock HTTP infrastructure, and sample canned API payloads
/// for TDD test classes without making live network requests.
/// Implements IDisposable to ensure clean resource disposal across test runs.
/// </summary>
public class RemoteApiTestFixture : IDisposable
{
    public HttpClient MockHttpClient { get; }
    public IGenshinApiClient ApiClient { get; }
    public RemoteCharacterData SampleCharacter { get; }
    public RemoteWeaponData SampleWeapon { get; }

    public RemoteApiTestFixture()
    {
        SampleCharacter = new RemoteCharacterData
        {
            Name = "Raiden Shogun",
            Title = "Plane of Euthymia",
            Vision = "Electro",
            Weapon = "Polearm",
            Rarity = 5,
            Description = "The Almighty Narukami Ogosho, seeking Eternity.",
            SkillTalents =
            [
                new RemoteSkillTalent { Name = "Origin", Unlock = "Normal Attack", Description = "5 rapid spear strikes.", Type = "Normal Attack" },
                new RemoteSkillTalent { Name = "Transcendence", Unlock = "Elemental Skill", Description = "Eye of Stormy Judgment.", Type = "Elemental Skill" },
                new RemoteSkillTalent { Name = "Secret Art: Musou Shinsetsu", Unlock = "Elemental Burst", Description = "Massive AoE Electro slash.", Type = "Elemental Burst" }
            ]
        };

        SampleWeapon = new RemoteWeaponData
        {
            Name = "Engulfing Lightning",
            Type = "Polearm",
            Rarity = 5,
            BaseAttack = 608,
            SubStat = "Energy Recharge",
            PassiveName = "Timeless Dream",
            PassiveDesc = "Increases attack based on energy recharge."
        };

        var handler = new MockHttpMessageHandler(new Dictionary<string, string>
        {
            ["https://genshin.jmp.blue/characters"] = JsonSerializer.Serialize(new[] { "raiden", "zhongli", "hu-tao", "diluc" }),
            ["https://genshin.jmp.blue/weapons"] = JsonSerializer.Serialize(new[] { "engulfing-lightning", "staff-of-homa", "wolfs-gravestone" }),
            ["https://genshin.jmp.blue/characters/raiden"] = JsonSerializer.Serialize(SampleCharacter),
            ["https://genshin.jmp.blue/weapons/engulfing-lightning"] = JsonSerializer.Serialize(SampleWeapon)
        });

        MockHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://genshin.jmp.blue/")
        };

        ApiClient = new GenshinApiClient(MockHttpClient);
    }

    public void Dispose()
    {
        MockHttpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private class MockHttpMessageHandler(Dictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString().TrimEnd('/') ?? string.Empty;

            if (responses.TryGetValue(uri, out var json))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
