namespace VoidRemove.Core.AppData;

/// <summary>
/// A JSON profile describing where one application stores its user data, loaded from
/// <c>data/profiles/*.json</c>. Profiles are original, community-extensible definitions.
/// </summary>
public sealed record AppDataProfile
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Names/tokens used to attach this profile to an installed program.</summary>
    public List<string> MatchNames { get; init; } = new();

    public List<DataLocationRule> Locations { get; init; } = new();

    /// <summary>Schema version of the profile file.</summary>
    public int Schema { get; init; } = 1;
}
