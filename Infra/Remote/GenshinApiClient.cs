using System.Net.Http.Json;
using System.Text.Json;
using Project.Infra.Remote.Models;

namespace Project.Infra.Remote;

/// <summary>
/// HTTP REST client communicating with the Genshin Impact public API.
/// Incorporates resilience fallback to an embedded catalog for reliable offline operation.
/// </summary>
public class GenshinApiClient : IGenshinApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GenshinApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://genshin.jmp.blue/");
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<IReadOnlyList<string>> GetCharacterNamesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<string>>("characters", JsonOptions, ct);
            if (response != null && response.Count > 0)
                return response;
        }
        catch (Exception)
        {
            // Graceful fallback to offline catalog
        }

        return FallbackCharacters.Keys.ToList();
    }

    public async Task<IReadOnlyList<string>> GetWeaponNamesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<string>>("weapons", JsonOptions, ct);
            if (response != null && response.Count > 0)
                return response;
        }
        catch (Exception)
        {
            // Graceful fallback to offline catalog
        }

        return FallbackWeapons.Keys.ToList();
    }

    public async Task<RemoteCharacterData?> GetCharacterAsync(string name, CancellationToken ct = default)
    {
        var slug = NormalizeSlug(name);
        try
        {
            var response = await _httpClient.GetFromJsonAsync<RemoteCharacterData>($"characters/{slug}", JsonOptions, ct);
            if (response != null)
                return response;
        }
        catch (Exception)
        {
            // Graceful fallback to offline catalog
        }

        return FallbackCharacters.GetValueOrDefault(slug) ?? FallbackCharacters.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RemoteWeaponData?> GetWeaponAsync(string name, CancellationToken ct = default)
    {
        var slug = NormalizeSlug(name);
        try
        {
            var response = await _httpClient.GetFromJsonAsync<RemoteWeaponData>($"weapons/{slug}", JsonOptions, ct);
            if (response != null)
                return response;
        }
        catch (Exception)
        {
            // Graceful fallback to offline catalog
        }

        return FallbackWeapons.GetValueOrDefault(slug) ?? FallbackWeapons.Values.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSlug(string input)
    {
        return input.Trim().ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
    }

    #region Offline Fallback Dataset
    private static readonly Dictionary<string, RemoteCharacterData> FallbackCharacters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["raiden"] = new RemoteCharacterData
        {
            Name = "Raiden Shogun",
            Title = "Plane of Euthymia",
            Vision = "Electro",
            Weapon = "Polearm",
            Rarity = 5,
            Description = "Her Excellency, the Almighty Narukami Ogosho, who promised the people of Inazuma an unchanging Eternity.",
            SkillTalents =
            [
                new RemoteSkillTalent { Name = "Origin", Unlock = "Normal Attack", Description = "Performs up to 5 consecutive spear strikes.", Type = "Normal Attack" },
                new RemoteSkillTalent { Name = "Transcendence: Baleful Omen", Unlock = "Elemental Skill", Description = "Unveils a shard of her Euthymia, dealing Electro DMG to nearby opponents and granting Eye of Stormy Judgment.", Type = "Elemental Skill" },
                new RemoteSkillTalent { Name = "Secret Art: Musou Shinsetsu", Unlock = "Elemental Burst", Description = "Unleashes the Musou no Hitotachi and deals AoE Electro DMG, entering the Musou Isshin state.", Type = "Elemental Burst" }
            ]
        },
        ["zhongli"] = new RemoteCharacterData
        {
            Name = "Zhongli",
            Title = "Vago Mundo",
            Vision = "Geo",
            Weapon = "Polearm",
            Rarity = 5,
            Description = "A mysterious consultant of Wangsheng Funeral Parlor, handsome, elegant, and possessed of extraordinarily vast knowledge.",
            SkillTalents =
            [
                new RemoteSkillTalent { Name = "Rain of Stone", Unlock = "Normal Attack", Description = "Performs up to 6 rapid spear strikes.", Type = "Normal Attack" },
                new RemoteSkillTalent { Name = "Dominus Lapidis", Unlock = "Elemental Skill", Description = "Commands the omnipresent power of earth to solidify into a Stone Stele and create a Jade Shield.", Type = "Elemental Skill" },
                new RemoteSkillTalent { Name = "Planet Befall", Unlock = "Elemental Burst", Description = "Brings a falling meteor down to the earth, dealing massive Geo DMG and applying Petrification.", Type = "Elemental Burst" }
            ]
        },
        ["hu-tao"] = new RemoteCharacterData
        {
            Name = "Hu Tao",
            Title = "Fragrance in Thaw",
            Vision = "Pyro",
            Weapon = "Polearm",
            Rarity = 5,
            Description = "The 77th Director of the Wangsheng Funeral Parlor. She took over the business at a rather young age.",
            SkillTalents =
            [
                new RemoteSkillTalent { Name = "Secret Spear of Wangsheng", Unlock = "Normal Attack", Description = "Performs up to 6 consecutive spear strikes.", Type = "Normal Attack" },
                new RemoteSkillTalent { Name = "Guide to Afterlife", Unlock = "Elemental Skill", Description = "Consumes a set portion of her HP to knock the surrounding enemies back and enter the Paramita Papilio state.", Type = "Elemental Skill" },
                new RemoteSkillTalent { Name = "Spirit Soother", Unlock = "Elemental Burst", Description = "Commands a blazing spirit to attack, dealing Pyro DMG in a large AoE.", Type = "Elemental Burst" }
            ]
        },
        ["diluc"] = new RemoteCharacterData
        {
            Name = "Diluc",
            Title = "The Darknight Hero",
            Vision = "Pyro",
            Weapon = "Claymore",
            Rarity = 5,
            Description = "The tycoon of a winery empire in Mondstadt, unmatched in every possible way.",
            SkillTalents =
            [
                new RemoteSkillTalent { Name = "Tempered Sword", Unlock = "Normal Attack", Description = "Performs up to 4 consecutive strikes.", Type = "Normal Attack" },
                new RemoteSkillTalent { Name = "Searing Onslaught", Unlock = "Elemental Skill", Description = "Performs a forward slash that deals Pyro DMG. Can be used 3 times consecutively.", Type = "Elemental Skill" },
                new RemoteSkillTalent { Name = "Dawn", Unlock = "Elemental Burst", Description = "Releases intense flames to summon a Phoenix that flies forward dealing massive Pyro DMG.", Type = "Elemental Burst" }
            ]
        }
    };

    private static readonly Dictionary<string, RemoteWeaponData> FallbackWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["engulfing-lightning"] = new RemoteWeaponData
        {
            Name = "Engulfing Lightning",
            Type = "Polearm",
            Rarity = 5,
            BaseAttack = 608,
            SubStat = "Energy Recharge",
            PassiveName = "Timeless Dream: Eternal Stove",
            PassiveDesc = "ATK increased by 28% of Energy Recharge over the base 100%. Gain 30% Energy Recharge for 12s after using an Elemental Burst."
        },
        ["staff-of-homa"] = new RemoteWeaponData
        {
            Name = "Staff of Homa",
            Type = "Polearm",
            Rarity = 5,
            BaseAttack = 608,
            SubStat = "CRIT DMG",
            PassiveName = "Reckless Cinnabar",
            PassiveDesc = "HP increased by 20%. Additionally, provides an ATK Bonus based on 0.8% of the wielder's Max HP."
        },
        ["wolfs-gravestone"] = new RemoteWeaponData
        {
            Name = "Wolf's Gravestone",
            Type = "Claymore",
            Rarity = 5,
            BaseAttack = 608,
            SubStat = "ATK",
            PassiveName = "Wolfish Tracker",
            PassiveDesc = "Increases ATK by 20%. On hit, attacks against opponents with less than 30% HP increase all party members' ATK by 40% for 12s."
        }
    };
    #endregion
}
