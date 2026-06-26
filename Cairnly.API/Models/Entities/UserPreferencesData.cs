namespace Cairnly.API.Models.Entities;

/// <summary>
/// Theme preferences for a user.
/// </summary>
public sealed record ThemePreferences
{
    /// <summary>Gets the color-scheme mode.</summary>
    public ThemeMode Mode { get; init; } = ThemeMode.System;

    /// <summary>Gets the selected accent color preset identifier (e.g. <c>green</c>).</summary>
    public string Accent { get; init; } = "green";
}

/// <summary>
/// The persisted, extensible set of user preferences, stored as jsonb. Add new preference groups
/// here over time without schema changes.
/// </summary>
public sealed record UserPreferencesData
{
    /// <summary>Gets the theme preferences.</summary>
    public ThemePreferences Theme { get; init; } = new();
}