using Project.Infra.Remote;
using Tests.Doubles;
using Tests.Fixtures;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for Remote REST API communication, data mapping, and structured text profiling.
/// Employs xUnit's IClassFixture<T> pattern for shared HTTP context.
/// Developed under the TDD (Test-Driven Development) methodology.
/// </summary>
public class RemoteApiTests(RemoteApiTestFixture fixture) : IClassFixture<RemoteApiTestFixture>
{
    private readonly RemoteApiTestFixture _fixture = fixture;

    [Fact]
    public async Task ApiClient_GetCharacterNames_ReturnsExpectedList()
    {
        // ==========================================
        // Arrange: Shared fixture provides mock HTTP client
        // ==========================================
        var client = _fixture.ApiClient;

        // ==========================================
        // Act: Fetch catalog of characters
        // ==========================================
        var names = await client.GetCharacterNamesAsync();

        // ==========================================
        // Assert: Catalog contains expected entries
        // ==========================================
        Assert.NotNull(names);
        Assert.Contains("raiden", names);
        Assert.Contains("zhongli", names);
    }

    [Fact]
    public async Task ApiClient_GetCharacter_DeserializesPayloadAccurately()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var client = _fixture.ApiClient;

        // ==========================================
        // Act
        // ==========================================
        var character = await client.GetCharacterAsync("raiden");

        // ==========================================
        // Assert
        // ==========================================
        Assert.NotNull(character);
        Assert.Equal("Raiden Shogun", character.Name);
        Assert.Equal("Electro", character.Vision);
        Assert.Equal(5, character.Rarity);
        Assert.Equal(3, character.SkillTalents?.Count);
    }

    [Fact]
    public async Task ApiClient_GetWeapon_DeserializesPayloadAccurately()
    {
        // ==========================================
        // Arrange
        // ==========================================
        var client = _fixture.ApiClient;

        // ==========================================
        // Act
        // ==========================================
        var weapon = await client.GetWeaponAsync("engulfing-lightning");

        // ==========================================
        // Assert
        // ==========================================
        Assert.NotNull(weapon);
        Assert.Equal("Engulfing Lightning", weapon.Name);
        Assert.Equal("Polearm", weapon.Type);
        Assert.Equal(608, weapon.BaseAttack);
        Assert.Equal(5, weapon.Rarity);
    }

    [Fact]
    public void RemoteDataMapper_ToCharacter_CalculatesRpgStatsAndAbilities()
    {
        // ==========================================
        // Arrange: 5-star character payload from fixture
        // ==========================================
        var rawData = _fixture.SampleCharacter;

        // ==========================================
        // Act: Map external DTO to internal domain Character entity
        // ==========================================
        var domainChar = RemoteDataMapper.ToCharacter(rawData, id: 101);

        // ==========================================
        // Assert: Verify derived stats and mapped abilities
        // ==========================================
        Assert.Equal(101, domainChar.Id);
        Assert.Equal("Raiden Shogun", domainChar.Name);
        Assert.Equal(175, domainChar.MaxHealth); // 100 + 5 * 15
        Assert.Equal(40, domainChar.Power);      // 20 + 5 * 4
        Assert.Equal(0.20, domainChar.Resistance); // 5 * 0.04
        Assert.Equal(3, domainChar.Abilities.Count);
        Assert.Contains(domainChar.Abilities, a => a.Name == "Secret Art: Musou Shinsetsu");
    }

    [Fact]
    public void RemoteDataMapper_ToItem_CalculatesWeaponBonuses()
    {
        // ==========================================
        // Arrange: 5-star weapon payload from fixture
        // ==========================================
        var rawWeapon = _fixture.SampleWeapon;

        // ==========================================
        // Act: Map external weapon DTO to internal domain Item entity
        // ==========================================
        var domainItem = RemoteDataMapper.ToItem(rawWeapon, id: 202);

        // ==========================================
        // Assert: Item stat bonuses conform to conversion formulas
        // ==========================================
        Assert.Equal(202, domainItem.Id);
        Assert.Equal("Engulfing Lightning", domainItem.Name);
        Assert.Equal(40, domainItem.HealthBonus); // 5 * 8
        Assert.Equal(48, domainItem.PowerBonus);  // 608 * 0.08
    }

    [Fact]
    public void CharacterTextProfileBuilder_BuildsStructuredTextDocument()
    {
        // ==========================================
        // Arrange: Setup domain character with equipped weapon
        // ==========================================
        var fakeDisplayer = new FakeDisplayer();
        var rawChar = _fixture.SampleCharacter;
        var rawWeapon = _fixture.SampleWeapon;

        var character = RemoteDataMapper.ToCharacter(rawChar, id: 1);
        var weapon = RemoteDataMapper.ToItem(rawWeapon, id: 1);
        character.AddItem(weapon);

        // ==========================================
        // Act: Build structured text document hierarchy
        // ==========================================
        var docManager = CharacterTextProfileBuilder.BuildProfile(character, rawChar, fakeDisplayer, rawWeapon);
        docManager.Print(whole: true);

        // ==========================================
        // Assert: Document structure contains lore, combat stats, and weapon details
        // ==========================================
        Assert.Equal("Raiden Shogun_Profile", docManager.Root.Name);
        Assert.True(fakeDisplayer.ContainsText("Plane of Euthymia"));
        Assert.True(fakeDisplayer.ContainsText("Combat Attributes"));
        Assert.True(fakeDisplayer.ContainsText("Engulfing Lightning"));
        Assert.True(fakeDisplayer.ContainsText("Secret Art: Musou Shinsetsu"));
    }
}
