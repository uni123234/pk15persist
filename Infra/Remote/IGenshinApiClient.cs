using Project.Infra.Remote.Models;

namespace Project.Infra.Remote;

/// <summary>
/// Service contract defining communication with the external game metadata REST API.
/// </summary>
public interface IGenshinApiClient
{
    Task<IReadOnlyList<string>> GetCharacterNamesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetWeaponNamesAsync(CancellationToken ct = default);
    Task<RemoteCharacterData?> GetCharacterAsync(string name, CancellationToken ct = default);
    Task<RemoteWeaponData?> GetWeaponAsync(string name, CancellationToken ct = default);
}
